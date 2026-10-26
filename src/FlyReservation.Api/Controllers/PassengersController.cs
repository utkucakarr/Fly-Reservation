using FluentValidation;
using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
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
        private readonly IValidator<CreatePassengerRequest> _createValidator;
        private readonly IValidator<UpdatePassengerRequest> _updateValidator;

        public PassengersController(
            AppDbContext context,
            IValidator<CreatePassengerRequest> createValidator,
            IValidator<UpdatePassengerRequest> updateValidator)
        {
            _context = context;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<ActionResult<List<PassengerResponse>>> GetAll()
        {
            var passengers = await _context.Passengers.ToListAsync();
            return Ok(passengers.Select(ToResponse).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PassengerResponse>> GetById(int id)
        {
            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger is null)
                return NotFound();

            return Ok(ToResponse(passenger));
        }

        [HttpPost]
        public async Task<ActionResult<PassengerResponse>> Create(CreatePassengerRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

            var passenger = new Passenger
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PassportNumber = request.PassportNumber
            };

            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = passenger.Id }, ToResponse(passenger));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdatePassengerRequest request)
        {
            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

            var passenger = await _context.Passengers.FindAsync(id);
            if (passenger is null)
                return NotFound();

            passenger.FirstName = request.FirstName;
            passenger.LastName = request.LastName;
            passenger.Email = request.Email;
            passenger.PassportNumber = request.PassportNumber;

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

        private static PassengerResponse ToResponse(Passenger passenger)
        {
            return new PassengerResponse(passenger.Id, passenger.FirstName, passenger.LastName, passenger.Email);
        }
    }
}
