using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IReservationService
    {
        Task<List<ReservationResponse>> GetAllAsync();
        Task<List<ReservationResponse>> GetMineAsync(CurrentUser user);
        Task<ReservationResponse> GetByIdAsync(int id, CurrentUser user);
        Task<ReservationResponse> CreateAsync(CreateReservationRequest request, string? idempotencyKey, CurrentUser user);
        Task DeleteAsync(int id, CurrentUser user);
        Task<ReservationPaymentResponse> PayAsync(int id, CurrentUser user);
        Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default);
    }
}
