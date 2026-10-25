using FlyReservation.Api.Data;
using FlyReservation.Api.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FlightsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Flight>>> GetAll()
        {
            var flights = await _context.Flights.ToListAsync();
            return Ok(flights);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Flight>> GetById(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight is null)
                return NotFound();

            return Ok(flight);
        }

        public async Task<ActionResult<Flight>> Create(Flight flight)
        {
            flight.AvailableSeats = flight.TotalSeats;

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = flight.Id }, flight);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Flight updatedFlight)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight is null)
                return NotFound();

            flight.FlightNumber = updatedFlight.FlightNumber;
            flight.Origin = updatedFlight.Origin;
            flight.Destination = updatedFlight.Destination;
            flight.DepartureTime = updatedFlight.DepartureTime;
            flight.ArrivalTime = updatedFlight.ArrivalTime;
            flight.TotalSeats = updatedFlight.TotalSeats;
            flight.Price = updatedFlight.Price;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight is null)
            {
                return NotFound();
            }

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
