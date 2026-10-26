using FluentValidation;
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
    public class FlightsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CreateFlightRequest> _createValidator;
        private readonly IValidator<UpdateFlightRequest> _updateValidator;

        public FlightsController(
            AppDbContext context,
            IValidator<CreateFlightRequest> createValidator,
            IValidator<UpdateFlightRequest> updateValidator)
        {
            _context = context;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        public async Task<ActionResult<List<FlightResponse>>> GetAll()
        {
            var flights = await _context.Flights.ToListAsync();
            return Ok(flights.Select(ToResponse).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FlightResponse>> GetById(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight is null)
                return NotFound();

            return Ok(ToResponse(flight));
        }

        [HttpPost]
        public async Task<ActionResult<FlightResponse>> Create(CreateFlightRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if(!validation.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

            var flight = new Flight
            {
                FlightNumber = request.FlightNumber,
                Origin = request.Origin,
                Destination = request.Destination,
                DepartureTime = request.DepartureTime,
                ArrivalTime = request.ArrivalTime,
                TotalSeats = request.TotalSeats,
                AvailableSeats = request.TotalSeats,
                Price = request.Price
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = flight.Id }, ToResponse(flight));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateFlightRequest request)
        {
            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

            var flight = await _context.Flights.FindAsync(id);
            if (flight is null)
                return NotFound();

            flight.FlightNumber = request.FlightNumber;
            flight.Origin = request.Origin;
            flight.Destination = request.Destination;
            flight.DepartureTime = request.DepartureTime;
            flight.ArrivalTime = request.ArrivalTime;
            flight.TotalSeats = request.TotalSeats;
            flight.Price = request.Price;

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

        private static FlightResponse ToResponse(Flight flight)
        {
            return new FlightResponse(
                flight.Id,
                flight.FlightNumber,
                flight.Origin,
                flight.Destination,
                flight.DepartureTime,
                flight.ArrivalTime,
                flight.TotalSeats,
                flight.AvailableSeats,
                flight.Price);
        }
    }
}
