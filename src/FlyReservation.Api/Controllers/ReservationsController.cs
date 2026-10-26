using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<ReservationResponse>>> GetAll()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Flight)
                .Include(r => r.Passenger)
                .ToListAsync();

            return Ok(reservations.Select(ToResponse).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationResponse>> GetById(int id)
        {
            var reservation = await FindWithDetailsAsync(id);
            if (reservation is null)
                return NotFound();

            return Ok(ToResponse(reservation));
        }

        [HttpPost]
        public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request)
        {
            var flightExists = await _context.Flights.AnyAsync(f => f.Id == request.FlightId);
            if (!flightExists)
                return NotFound(new ProblemDetails { Title = $"Flight {request.FlightId} not found." });

            var passengerExists = await _context.Passengers.AnyAsync(p => p.Id == request.PassengerId);
            if (!passengerExists)
                return NotFound(new ProblemDetails { Title = $"Passenger {request.PassengerId} not found." });

            // Object Initializer
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

            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, ToResponse(created!));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation is null)
                return NotFound();

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return NoContent();
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