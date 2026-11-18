using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
    // [ProducesResponseType] is for the OpenAPI document: it only sees the method signature, not the
    // exceptions the services throw. Once an action lists any code, it must list its success code too
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]     // every endpoint needs a logged-in user; the admin-only ones add their role on top
    public class PassengersController : ControllerBase
    {
        private readonly IPassengerService _passengerService;

        public PassengersController(IPassengerService passengerService)
        {
            _passengerService = passengerService;
        }

        // Every passenger's name and email: not for the public
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]      // a valid token, but not an admin
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PassengerResponse>>> GetAll()
        {
            return Ok(await _passengerService.GetAllAsync());
        }

        // The logged-in user's own passengers: themselves, family, colleagues
        [HttpGet("mine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        public async Task<ActionResult<List<PassengerResponse>>> GetMine()
        {
            return Ok(await _passengerService.GetMineAsync(CurrentUser.From(User)));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]     // unknown, or someone else's
        public async Task<ActionResult<PassengerResponse>> GetById(int id)
        {
            var (passenger, version) = await _passengerService.GetByIdAsync(id, CurrentUser.From(User));

            // The client sends this back in If-Match when it updates the passenger
            Response.Headers.ETag = ETags.From(version);
            return Ok(passenger);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // the email is already used
        public async Task<ActionResult<PassengerResponse>> Create(CreatePassengerRequest request)
        {
            var created = await _passengerService.CreateAsync(request, CurrentUser.From(User));
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]     // unknown, or someone else's
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // the email is already used
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]   // changed by someone else after the client's GET
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)] // no If-Match header
        public async Task<IActionResult> Update(int id, UpdatePassengerRequest request, [FromHeader(Name = "If-Match")] string? ifMatch)
        {
            var newVersion = await _passengerService.UpdateAsync(id, request, ETags.ParseIfMatch(ifMatch), CurrentUser.From(User));

            // The new version, so the client can save again without another GET
            Response.Headers.ETag = ETags.From(newVersion);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]      // a valid token, but not an admin
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
