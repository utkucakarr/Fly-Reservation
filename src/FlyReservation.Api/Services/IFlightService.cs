using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IFlightService
    {
        Task<List<FlightResponse>> GetAllAsync();
        Task<FlightResponse> GetByIdAsync(int id);
        Task<FlightResponse> CreateAsync(CreateFlightRequest request);
        Task UpdateAsync(int id, UpdateFlightRequest request);
        Task DeleteAsync(int id);
    }
}
