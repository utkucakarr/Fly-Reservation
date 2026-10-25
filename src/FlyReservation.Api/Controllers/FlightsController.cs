using FlyReservation.Api.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private static readonly List<Flight> _flights = new();
        private static int _nextId = 1;

        [HttpGet]
        public ActionResult<List<Flight>> GetAll()
        {
            return Ok(_flights);
        }

        [HttpGet("{id}")]
        public ActionResult<Flight> GetById(int id)
        {
            var flight = _flights.FirstOrDefault(f => f.Id == id);
            if (flight is null)
                return NotFound();

            return Ok(flight);
        }

        [HttpPost]
        public ActionResult<Flight> Create(Flight flight)
        {
            flight.Id = _nextId++;
            flight.AvailableSeats = flight.TotalSeats;
            _flights.Add(flight);

            return CreatedAtAction(nameof(GetById), new { id = flight.Id }, flight);
        }

        [HttpPut("{id}")]
        public ActionResult Update(int id, Flight updatedFlight)
        {
            var flight = _flights.FirstOrDefault(f => f.Id == id);
            if (flight is null)
                return NotFound();

            flight.FlightNumber = updatedFlight.FlightNumber;
            flight.Origin = updatedFlight.Origin;
            flight.Destination = updatedFlight.Destination;
            flight.DepartureTime = updatedFlight.DepartureTime;
            flight.ArrivalTime = updatedFlight.ArrivalTime;
            flight.TotalSeats = updatedFlight.TotalSeats;
            flight.Price = updatedFlight.Price;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var flight = _flights.FirstOrDefault(f => f.Id == id);
            if (flight is null)
                return NotFound();

            _flights.Remove(flight);

            return NoContent();
        }
    }
}
