namespace FlyReservation.Api.Dtos
{
    public record LoginResponse(string AccessToken, DateTime ExpiresAt);
}
