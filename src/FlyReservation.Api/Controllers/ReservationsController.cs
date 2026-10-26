using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ReservationResponse>>> GetAll()
        {
            return Ok(await _reservationService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationResponse>> GetById(int id)
        {
            return Ok(await _reservationService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request)
        {
            var created = await _reservationService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _reservationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
