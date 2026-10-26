using FlyReservation.Api.Dtos;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
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
        public async Task<ActionResult<PassengerResponse>> GetById(int id)
        {
            return Ok(await _passengerService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult<PassengerResponse>> Create(CreatePassengerRequest request)
        {
            var created = await _passengerService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdatePassengerRequest request)
        {
            await _passengerService.UpdateAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _passengerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
