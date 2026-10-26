using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IPassengerService
    {
        Task<List<PassengerResponse>> GetAllAsync();
        Task<PassengerResponse> GetByIdAsync(int id);
        Task<PassengerResponse> CreateAsync(CreatePassengerRequest request);
        Task UpdateAsync(int id, UpdatePassengerRequest request);
        Task DeleteAsync(int id);
    }
}
