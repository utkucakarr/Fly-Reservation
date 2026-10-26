using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IReservationService
    {
        Task<List<ReservationResponse>> GetAllAsync();
        Task<ReservationResponse> GetByIdAsync(int id);
        Task<ReservationResponse> CreateAsync(CreateReservationRequest request);
        Task DeleteAsync(int id);
    }
}
