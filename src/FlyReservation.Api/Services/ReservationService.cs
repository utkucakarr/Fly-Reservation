using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Services
{
    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(AppDbContext context, ILogger<ReservationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ReservationResponse>> GetAllAsync()
        {
            var reservations = await _context.Reservations
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

        public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request)
        {
            // Read only to check that the flight exists and to get its number for the error message (no tracking)
            var flight = await _context.Flights.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.FlightId)
                ?? throw new NotFoundException($"Flight {request.FlightId} not found.");

            var passengerExists = await _context.Passengers.AnyAsync(p => p.Id == request.PassengerId);
            if (!passengerExists)
                throw new NotFoundException($"Passenger {request.PassengerId} not found.");

            var alreadyBooked = await _context.Reservations
                .AnyAsync(r => r.FlightId == request.FlightId && r.PassengerId == request.PassengerId);
            if(alreadyBooked)
                throw new ConflictException($"Passenger {request.PassengerId} already has a reservation on flight {flight.FlightNumber}.");

            // The seat decrement and the reservation insert must succeed or fail together
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Atomic update: check and decrement in ONE statement, so two requests can never take the same last seat
            var reservedSeats = await _context.Flights
                .Where(f => f.Id == request.FlightId && f.AvailableSeats > 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.AvailableSeats, f => f.AvailableSeats - 1));

            if (reservedSeats == 0)
                throw new ConflictException($"Flight {flight.FlightNumber} has no available seats.");

            var reservation = new Reservation
            {
                FlightId = request.FlightId,
                PassengerId = request.PassengerId,
                PnrCode = GeneratePnrCode(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var created = await FindWithDetailsAsync(reservation.Id);

            _logger.LogInformation(
                "Reservation {PnrCode} created on flight {FlightNumber} for passenger {PassengerId}. {AvailableSeats} seats left",
                created!.PnrCode, created.Flight.FlightNumber, request.PassengerId, created.Flight.AvailableSeats);

            return ToResponse(created);
        }

        public async Task DeleteAsync(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Flight)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException($"Reservation {id} not found.");

            reservation.Flight.ReleaseSeat();

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Reservation {PnrCode} cancelled on flight {FlightNumber}. {AvailableSeats} seats left",
                reservation.PnrCode, reservation.Flight.FlightNumber, reservation.Flight.AvailableSeats);
        }

        private async Task<Reservation?> FindWithDetailsAsync(int id)
        {
            return await _context.Reservations
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
    }
}
