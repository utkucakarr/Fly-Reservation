using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
    // [ProducesResponseType] is for the OpenAPI document: it only sees the method signature, not the
    // exceptions the services throw. Once an action lists any code, it must list its success code too
    [Route("api/[controller]")]
    [ApiController]
    public class PassengersController : ControllerBase
    {
        private readonly IPassengerService _passengerService;

        public PassengersController(IPassengerService passengerService)
        {
            _passengerService = passengerService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PassengerResponse>>> GetAll()
        {
            return Ok(await _passengerService.GetAllAsync());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PassengerResponse>> GetById(int id)
        {
            return Ok(await _passengerService.GetByIdAsync(id));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // the email is already used
        public async Task<ActionResult<PassengerResponse>> Create(CreatePassengerRequest request)
        {
            var created = await _passengerService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // the email is already used
        public async Task<IActionResult> Update(int id, UpdatePassengerRequest request)
        {
            await _passengerService.UpdateAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // the passenger still has reservations
        public async Task<IActionResult> Delete(int id)
        {
            await _passengerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
