using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IFlightService
    {
        Task<PagedResponse<FlightResponse>> GetAllAsync(FlightListRequest request);
        Task<(FlightResponse Flight, int Version)> GetByIdAsync(int id);
        Task<FlightResponse> CreateAsync(CreateFlightRequest request);
        Task<int> UpdateAsync(int id, UpdateFlightRequest request, int expectedVersion);
        Task DeleteAsync(int id);
        Task<List<FlightResponse>> SearchAsync(FlightSearchRequest request);
    }
}
