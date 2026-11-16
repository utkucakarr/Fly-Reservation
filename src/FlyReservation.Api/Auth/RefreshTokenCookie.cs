namespace FlyReservation.Api.Auth
{
    public static class RefreshTokenCookie
    {
        public const string Name = "refreshToken";

        // The browser sends the cookie only to these URLs, so no other endpoint ever sees the refresh token
        private const string Path = "/api/auth";

        public static void Append(HttpResponse response, string refreshToken, DateTime expiresAt)
        {
            response.Cookies.Append(Name, refreshToken, Options(expiresAt));
        }

        public static void Delete(HttpResponse response)
        {
            // The browser only removes a cookie when the attributes match the ones it was set with
            response.Cookies.Delete(Name, Options(expiresAt: null));
        }

        private static CookieOptions Options(DateTime? expiresAt) => new()
        {
            HttpOnly = true,                    // XSS: JavaScript on the page cannot read the token
            Secure = true,                      // never sent over plain HTTP
            SameSite = SameSiteMode.Strict,     // CSRF: not sent with requests that start on another site
            Path = Path,
            Expires = expiresAt
        };
    }
}
