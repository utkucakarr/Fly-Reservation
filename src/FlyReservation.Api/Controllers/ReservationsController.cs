using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FlyReservation.Api.Controllers
{
    // [ProducesResponseType] is for the OpenAPI document: it only sees the method signature, not the
    // exceptions the services throw. Once an action lists any code, it must list its success code too
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]     // every endpoint needs a logged-in user; the admin-only ones add their role on top
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // Every reservation of every passenger: not for the public
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]      // a valid token, but not an admin
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ReservationResponse>>> GetAll()
        {
            return Ok(await _reservationService.GetAllAsync());
        }

        // The logged-in user's own bookings, newest first
        [HttpGet("mine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        public async Task<ActionResult<List<ReservationResponse>>> GetMine()
        {
            return Ok(await _reservationService.GetMineAsync(CurrentUser.From(User)));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]     // unknown, or someone else's
        public async Task<ActionResult<ReservationResponse>> GetById(int id)
        {
            return Ok(await _reservationService.GetByIdAsync(id, CurrentUser.From(User)));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]                     // also for a retry with the same Idempotency-Key
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]     // unknown flight, or a passenger that is unknown or someone else's
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // no seats left, or the passenger is already booked
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)] // the key was used for a different request
        public async Task<ActionResult<ReservationResponse>> Create(
            CreateReservationRequest request,
            [FromHeader(Name = "Idempotency-Key"), MaxLength(100)] string? idempotencyKey)
        {
            var created = await _reservationService.CreateAsync(request, idempotencyKey, CurrentUser.From(User));
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]     // unknown, or someone else's
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // already paid or expired
        public async Task<IActionResult> Delete(int id)
        {
            await _reservationService.DeleteAsync(id, CurrentUser.From(User));
            return NoContent();
        }

        [HttpPost("{id}/payment")]
        [ProducesResponseType(StatusCodes.Status200OK)]                          // also when it was already paid
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no token, or an invalid or expired one
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]     // unknown, or someone else's
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]     // expired, or cancelled during the payment
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)] // payment provider unreachable
        public async Task<ActionResult<ReservationPaymentResponse>> Pay(int id)
        {
            return Ok(await _reservationService.PayAsync(id, CurrentUser.From(User)));
        }
    }
}
