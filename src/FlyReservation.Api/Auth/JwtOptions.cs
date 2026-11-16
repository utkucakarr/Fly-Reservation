namespace FlyReservation.Api.Auth
{
    // The "Jwt" section. Issuer, Audience and lifetime are in appsettings.json; Key is a secret (user secrets / environment)
    public class JwtOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        // Signs and verifies every token (HMAC-SHA256). Whoever knows it can make themselves an admin
        public string Key { get; set; } = string.Empty;

        // Short on purpose: an access token cannot be taken back before it expires
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

        // How long a login lasts without the password. Refresh tokens are stored, so they can be revoked
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    }
}
