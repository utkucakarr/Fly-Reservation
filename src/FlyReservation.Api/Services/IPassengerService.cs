using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IPassengerService
    {
        Task<List<PassengerResponse>> GetAllAsync();
        Task<(PassengerResponse Passenger, int Version)> GetByIdAsync(int id);
        Task<PassengerResponse> CreateAsync(CreatePassengerRequest request);
        Task<int> UpdateAsync(int id, UpdatePassengerRequest request, int expectedVersion);
        Task DeleteAsync(int id);
    }
}
