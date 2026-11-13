using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
    // [ProducesResponseType] is for the OpenAPI document: it only sees the method signature, not the
    // exceptions the services throw. Once an action lists any code, it must list its success code too
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightService _flightService;

        public FlightsController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<FlightResponse>>> GetAll([FromQuery] FlightListRequest request)
        {
            return Ok(await _flightService.GetAllAsync(request));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FlightResponse>> GetById(int id)
        {
            var (flight, version) = await _flightService.GetByIdAsync(id);

            // The client sends this back in If-Match when it updates the flight
            Response.Headers.ETag = ETags.From(version);
            return Ok(flight);
        }

        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<FlightResponse>>> Search([FromQuery] FlightSearchRequest request)
        {
            return Ok(await _flightService.SearchAsync(request));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // same flight number and departure time
        public async Task<ActionResult<FlightResponse>> Create(CreateFlightRequest request)
        {
            var created = await _flightService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // same flight number and departure time as another flight
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]   // changed by someone else after the client's GET
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)] // no If-Match header
        public async Task<IActionResult> Update(int id, UpdateFlightRequest request, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var newVersion = await _flightService.UpdateAsync(id, request, ETags.ParseIfMatch(ifMatch));

            // The new version, so the client can save again without another GET
            Response.Headers.ETag = ETags.From(newVersion);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // the flight still has reservations
        public async Task<IActionResult> Delete(int id)
        {
            await _flightService.DeleteAsync(id);
            return NoContent();
        }
    }
}
