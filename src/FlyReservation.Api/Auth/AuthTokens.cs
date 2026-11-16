namespace FlyReservation.Api.Auth
{
    // What a login or a refresh produces. The access token goes into the response body,
    // the refresh token into an HttpOnly cookie
    public record AuthTokens(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
