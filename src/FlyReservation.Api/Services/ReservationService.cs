using FlyReservation.Api.Caching;
using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;
using FlyReservation.Api.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;
using Microsoft.Extensions.Options;

namespace FlyReservation.Api.Services
{
    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReservationService> _logger;
        private readonly HybridCache _cache;
        private readonly IPaymentClient _paymentClient;
        // Longer than the payment client's total timeout (10 s), so the expiry job can't take the seat during a charge
        private static readonly TimeSpan PaymentClaimDuration = TimeSpan.FromMinutes(1);
        private const int ExpiryBatchSize = 100;
        private readonly ReservationOptions _options;

        public ReservationService(
            AppDbContext context,
            ILogger<ReservationService> logger,
            HybridCache cache,
            IPaymentClient paymentClient,
            IOptions<ReservationOptions> options)
        { 
            _context = context;
            _logger = logger;
            _cache = cache;
            _paymentClient = paymentClient;
            _options = options.Value;
        }

        public async Task<List<ReservationResponse>> GetAllAsync()
        {
            // Read only: no change tracking needed
            var reservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Flight)
                .Include(r => r.Passenger)
                .ToListAsync();

            return reservations.Select(ToResponse).ToList();
        }

        public async Task<ReservationResponse> GetByIdAsync(int id)
        {
            var reservation = await FindWithDetailsAsync(id)
                ?? throw new NotFoundException($"Reservation {id} not found.");

            return ToResponse(reservation);
        }

        public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request, string? idempotencyKey)
        {
            // A retry with a known key gets the first result back instead of booking again
            if(idempotencyKey is not null)
            {
                var previous = await FindByIdempotencyKeyAsync(idempotencyKey);
                if (previous is not null)
                    return ToReplayResponse(previous, request);
            }

            // Read only to check that the flight exists and to get its number for the error message (no tracking)
            var flight = await _context.Flights.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.FlightId)
                ?? throw new NotFoundException($"Flight {request.FlightId} not found.");

            var passengerExists = await _context.Passengers.AnyAsync(p => p.Id == request.PassengerId);
            if (!passengerExists)
                throw new NotFoundException($"Passenger {request.PassengerId} not found.");

            var alreadyBooked = await _context.Reservations
                .AnyAsync(r => r.FlightId == request.FlightId
                    && r.PassengerId == request.PassengerId
                    && r.Status != ReservationStatus.Expired);
            if (alreadyBooked)
                throw new ConflictException($"Passenger {request.PassengerId} already has a reservation on flight {flight.FlightNumber}.");

            // The seat decrement and the reservation insert must succeed or fail together
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var reservation = new Reservation
            {
                FlightId = request.FlightId,
                PassengerId = request.PassengerId,
                PnrCode = GeneratePnrCode(),
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = idempotencyKey,
                Status = ReservationStatus.PendingPayment,
                PaymentDueAt = DateTime.UtcNow.Add(_options.PaymentWindow),
                Price = flight.Price // <- snapshot
            };

            // Insert BEFORE taking the seat: a concurrent request with the same key waits on the unique index
            // until we commit, then fails there instead of competing for a seat
            _context.Reservations.Add(reservation);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (idempotencyKey is not null && IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                // Null means a different unique rule failed (e.g. the passenger already has a seat) → 409 from the handler
                var winner = await FindByIdempotencyKeyAsync(idempotencyKey);
                if (winner is null)
                    throw;

                return ToReplayResponse(winner, request);
            }

            // Atomic update: check and decrement in ONE statement, so two requests can never take the same last seat
            var reservedSeats = await _context.Flights
                .Where(f => f.Id == request.FlightId && f.AvailableSeats > 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.AvailableSeats, f => f.AvailableSeats - 1));

            // Not committed → the reservation inserted above is rolled back too
            if (reservedSeats == 0)
                throw new ConflictException($"Flight {flight.FlightNumber} has no available seats.");

            await transaction.CommitAsync();

            // A seat was taken: cached search results now show the wrong AvailableSeats.
            // Only after the commit, so a search in between cannot cache the old value again
            await _cache.RemoveByTagAsync(CacheTags.FlightSearch);

            var created = await FindWithDetailsAsync(reservation.Id);

            _logger.LogInformation(
                "Reservation {PnrCode} created on flight {FlightNumber} for passenger {PassengerId}. {AvailableSeats} seats left",
                created!.PnrCode, created.Flight.FlightNumber, request.PassengerId, created.Flight.AvailableSeats);

            return ToResponse(created);
        }

        public async Task DeleteAsync(int id)
        {
            var reservation = await _context.Reservations
                   .AsNoTracking()
                   .FirstOrDefaultAsync(r => r.Id == id)
                   ?? throw new NotFoundException($"Reservation {id} not found.");

            // Removing the row and giving the seat back must succeed or fail together
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Only pending ones: an expired reservation gave its seat back already, a confirmed one needs a refund first
            var deleted = await _context.Reservations
                .Where(r => r.Id == id && r.Status == ReservationStatus.PendingPayment)
                .ExecuteDeleteAsync();

            if (deleted == 0)
                throw new ConflictException($"Reservation {reservation.PnrCode} can no longer be cancelled.");

            await ReleaseSeatAsync(reservation.FlightId);
            await transaction.CommitAsync();
            await _cache.RemoveByTagAsync(CacheTags.FlightSearch);

            _logger.LogInformation("Reservation {PnrCode} cancelled", reservation.PnrCode);
        }

        public async Task<ReservationPaymentResponse> PayAsync(int id)
        {
            var reservation = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException($"Reservation {id} not found.");

            // Paying twice is not an error, and it must not reach the provider again
            if (reservation.Status == ReservationStatus.Confirmed)
                return new ReservationPaymentResponse(reservation.PnrCode, reservation.Price, reservation.PaymentTransactionId);

            // Claim before charging: makes sure the deadline is not about to pass. Never shortens it.
            // The same WHERE as the expiry job, so for one row exactly one of them wins
            var now = DateTime.UtcNow;
            DateTime? claimedUntil = now.Add(PaymentClaimDuration);
            var claimed = await _context.Reservations
                .Where(r => r.Id == id && r.Status == ReservationStatus.PendingPayment && r.PaymentDueAt > now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    r => r.PaymentDueAt,
                    r => r.PaymentDueAt < claimedUntil ? claimedUntil : r.PaymentDueAt));

            if (claimed == 0)
                throw new ConflictException($"Reservation {reservation.PnrCode} has expired. Please book again.");

            var transactionId = await _paymentClient.ChargeAsync($"payment-{reservation.PnrCode}", reservation.Price);

            var confirmed = await _context.Reservations
                .Where(r => r.Id == id && r.Status == ReservationStatus.PendingPayment)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, ReservationStatus.Confirmed)
                    .SetProperty(r => r.PaymentDueAt, (DateTime?)null)
                    .SetProperty(r => r.PaymentTransactionId, transactionId));

            if (confirmed == 0 && !await _context.Reservations.AnyAsync(r => r.Id == id && r.Status == ReservationStatus.Confirmed))
            {
                // Cancelled while the charge was running: the money is taken, the reservation is gone
                _logger.LogError("Reservation {PnrCode} was charged ({TransactionId}) but is no longer pending. Refund needed",
                    reservation.PnrCode, transactionId);
                throw new ConflictException($"Reservation {reservation.PnrCode} is no longer active.");
            }

            _logger.LogInformation("Reservation {PnrCode} paid {Amount}. Transaction {TransactionId}",
                reservation.PnrCode, reservation.Price, transactionId);

            return new ReservationPaymentResponse(reservation.PnrCode, reservation.Price, transactionId);
        }

        public async Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var overdue = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.Status == ReservationStatus.PendingPayment && r.PaymentDueAt < now)
                .OrderBy(r => r.PaymentDueAt)
                .Take(ExpiryBatchSize)
                .Select(r => new { r.Id, r.FlightId, r.PnrCode })
                .ToListAsync(cancellationToken);

            var expiredCount = 0;

            foreach (var reservation in overdue)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                // Status is checked again in the UPDATE: a payment or another API instance may have got here first.
                // Then 0 rows change, and the seat is not released a second time
                var expired = await _context.Reservations
                    .Where(r => r.Id == reservation.Id && r.Status == ReservationStatus.PendingPayment && r.PaymentDueAt < now)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, ReservationStatus.Expired), cancellationToken);

                if (expired == 0)
                    continue;

                await ReleaseSeatAsync(reservation.FlightId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                expiredCount++;

                _logger.LogInformation("Reservation {PnrCode} expired, seat released", reservation.PnrCode);
            }

            if (expiredCount > 0)
                await _cache.RemoveByTagAsync(CacheTags.FlightSearch, cancellationToken);

            return expiredCount;
        }

        private async Task<Reservation?> FindWithDetailsAsync(int id)
        {
            return await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Flight)
                .Include(r => r.Passenger)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Mapping operation Entity to Dto
        private static ReservationResponse ToResponse(Reservation reservation)
        {
            return new ReservationResponse(
                reservation.Id,
                reservation.PnrCode,
                reservation.Status.ToString(),
                reservation.Price,
                reservation.PaymentDueAt,
                reservation.CreatedAt,
                reservation.FlightId,
                reservation.Flight.FlightNumber,
                reservation.Flight.Origin,
                reservation.Flight.Destination,
                reservation.Flight.DepartureTime,
                reservation.PassengerId,
                $"{reservation.Passenger.FirstName} {reservation.Passenger.LastName}");
        }

        private static string GeneratePnrCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            return new string(Enumerable.Range(0, 6)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
                .ToArray());
        }

        private async Task<Reservation?> FindByIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Flight)
                .Include(r => r.Passenger)
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);
        }

        private static ReservationResponse ToReplayResponse(Reservation previous, CreateReservationRequest request)
        {
            if (previous.FlightId != request.FlightId || previous.PassengerId != request.PassengerId)
                throw new IdempotencyKeyReusedException("This Idempotency-Key was already used for a different reservation request.");

            return ToResponse(previous);
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
        }

        // Atomic: another booking or expiry changing the same flight can't be overwritten
        private async Task ReleaseSeatAsync(int flightId, CancellationToken cancellationToken = default)
        {
            await _context.Flights
                .Where(f => f.Id == flightId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.AvailableSeats, f => f.AvailableSeats + 1), cancellationToken);
        }
    }
}
