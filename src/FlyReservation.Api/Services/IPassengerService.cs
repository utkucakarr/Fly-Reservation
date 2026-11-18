using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IPassengerService
    {
        Task<List<PassengerResponse>> GetAllAsync();
        Task<List<PassengerResponse>> GetMineAsync(CurrentUser user);
        Task<(PassengerResponse Passenger, int Version)> GetByIdAsync(int id, CurrentUser user);
        Task<PassengerResponse> CreateAsync(CreatePassengerRequest request, CurrentUser user);
        Task<int> UpdateAsync(int id, UpdatePassengerRequest request, int expectedVersion, CurrentUser user);
        Task DeleteAsync(int id);
    }
}
