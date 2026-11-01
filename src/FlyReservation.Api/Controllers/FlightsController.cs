using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
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
        public async Task<ActionResult<PagedResponse<FlightResponse>>> GetAll([FromQuery] FlightListRequest request)
        {
            return Ok(await _flightService.GetAllAsync(request));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FlightResponse>> GetById(int id)
        {
            return Ok(await _flightService.GetByIdAsync(id));
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<FlightResponse>>> Search([FromQuery] FlightSearchRequest request)
        {
            return Ok(await _flightService.SearchAsync(request));
        }

        [HttpPost]
        public async Task<ActionResult<FlightResponse>> Create(CreateFlightRequest request)
        {
            var created = await _flightService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateFlightRequest request)
        {
            await _flightService.UpdateAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _flightService.DeleteAsync(id);
            return NoContent();
        }
    }
}
