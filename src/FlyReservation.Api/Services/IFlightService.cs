using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IFlightService
    {
        Task<PagedResponse<FlightResponse>> GetAllAsync(FlightListRequest request);
        Task<FlightResponse> GetByIdAsync(int id);
        Task<FlightResponse> CreateAsync(CreateFlightRequest request);
        Task UpdateAsync(int id, UpdateFlightRequest request);
        Task DeleteAsync(int id);
    }
}
