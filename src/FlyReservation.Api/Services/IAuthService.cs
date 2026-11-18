using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Services
{
    public interface IAuthService
    {
        Task<AuthTokens> LoginAsync(LoginRequest request);
        Task<AuthTokens> RefreshAsync(string? refreshToken);
        Task LogoutAsync(string? refreshToken);
        Task<AuthTokens> RegisterAsync(RegisterRequest request);
    }
}
