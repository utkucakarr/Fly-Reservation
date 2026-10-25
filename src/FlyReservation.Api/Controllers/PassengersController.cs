using FlyReservation.Api.Data;
using FlyReservation.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PassengersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PassengersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Passenger>>> GetAll()
        {
            var passengers = await _context.Passengers.ToListAsync();
            return Ok(passengers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Passenger>> GetById(int id)
        {
            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger is null)
                return NotFound();

            return Ok(passenger);
        }

        [HttpPost]
        public async Task<ActionResult<Passenger>> Create(Passenger passenger)
        {
            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = passenger.Id }, passenger);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Passenger updatedPassenger)
        {
            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger is null)
                return NotFound();

            passenger.FirstName = updatedPassenger.FirstName;
            passenger.LastName = updatedPassenger.LastName;
            passenger.Email = updatedPassenger.Email;
            passenger.PassportNumber = updatedPassenger.PassportNumber;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger is null)
                return NotFound();

            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
