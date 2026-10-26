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

        public ReservationService(AppDbContext context)
        {
            _context = context;
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
            var flightExists = await _context.Flights.AnyAsync(f => f.Id == request.FlightId);
            if (!flightExists)
                throw new NotFoundException($"Flight {request.FlightId} not found.");

            var passengerExists = await _context.Passengers.AnyAsync(p => p.Id == request.PassengerId);
            if (!passengerExists)
                throw new NotFoundException($"Passenger {request.PassengerId} not found.");

            var reservation = new Reservation
            {
                FlightId = request.FlightId,
                PassengerId = request.PassengerId,
                PnrCode = GeneratePnrCode(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            var created = await FindWithDetailsAsync(reservation.Id);
            return ToResponse(created!);
        }

        public async Task DeleteAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id)
                ?? throw new NotFoundException($"Reservation {id} not found.");

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
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
