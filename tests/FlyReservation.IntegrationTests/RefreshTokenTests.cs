using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlyReservation.Api.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlyReservation.IntegrationTests
{
    public class RefreshTokenTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public RefreshTokenTests(FlyReservationApiFactory factory)
        {
            // No cookie jar: the tests carry the refresh token by hand, so they can also send an old one again
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        }

        [Fact]
        public async Task Login_PutsTheRefreshTokenInAHardenedCookieAndNotInTheBody()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(ApiTestHelpers.AdminEmail, ApiTestHelpers.AdminPassword));

            var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
            Assert.StartsWith("refreshToken=", setCookie);
            Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);         // XSS: scripts cannot read it
            Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);           // HTTPS only
            Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);  // CSRF: not sent from other sites
            Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);   // only the auth endpoints see it

            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("refresh", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        }

        [Fact]
        public async Task Refresh_ReturnsAWorkingAccessTokenAndANewRefreshToken()
        {
            var (_, refreshToken) = await LoginAsync();

            var response = await RefreshAsync(refreshToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEqual(refreshToken, ReadRefreshToken(response));       // rotation
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            var adminOnly = new HttpRequestMessage(HttpMethod.Get, "/api/passengers");
            adminOnly.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(adminOnly)).StatusCode);
        }

        // A refresh token works once. If a used one comes back, either the thief or the real user is holding a
        // stolen copy, and the server cannot tell which: every token from that login stops working
        [Fact]
        public async Task Refresh_WithAnAlreadyUsedToken_Returns401AndRevokesTheNewTokenToo()
        {
            var (_, stolen) = await LoginAsync();
            var current = ReadRefreshToken(await RefreshAsync(stolen));     // the real user refreshes: "stolen" is now used

            var replay = await RefreshAsync(stolen);
            var afterReplay = await RefreshAsync(current);

            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
        }

        // The same check-then-act race as overbooking: "is this token unused?" and "mark it used" must be one step
        [Fact]
        public async Task Refresh_SameTokenConcurrently_OnlyOneSucceeds()
        {
            var (_, refreshToken) = await LoginAsync();

            var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => RefreshAsync(refreshToken)));

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(19, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));
        }

        [Fact]
        public async Task Refresh_WithoutCookie_Returns401()
        {
            var response = await RefreshAsync(refreshToken: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // CSRF, second layer: a form on another site can make the browser send the cookie, but not a custom header
        [Fact]
        public async Task Refresh_WithoutCsrfHeader_Returns400AndDoesNotUseTheToken()
        {
            var (_, refreshToken) = await LoginAsync();

            var withoutHeader = await RefreshAsync(refreshToken, csrfHeader: false);
            var withHeader = await RefreshAsync(refreshToken);

            Assert.Equal(HttpStatusCode.BadRequest, withoutHeader.StatusCode);
            Assert.Equal(HttpStatusCode.OK, withHeader.StatusCode);
        }

        [Fact]
        public async Task Logout_RevokesTheRefreshTokenAndClearsTheCookie()
        {
            var (_, refreshToken) = await LoginAsync();

            var logout = await SendWithCookieAsync("/api/auth/logout", refreshToken, csrfHeader: true);
            var afterLogout = await RefreshAsync(refreshToken);

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.StartsWith("refreshToken=;", Assert.Single(logout.Headers.GetValues("Set-Cookie")));
            Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        }

        private async Task<(string AccessToken, string RefreshToken)> LoginAsync()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(ApiTestHelpers.AdminEmail, ApiTestHelpers.AdminPassword));
            response.EnsureSuccessStatusCode();

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return (login!.AccessToken, ReadRefreshToken(response));
        }

        private Task<HttpResponseMessage> RefreshAsync(string? refreshToken, bool csrfHeader = true)
            => SendWithCookieAsync("/api/auth/refresh", refreshToken, csrfHeader);

        // What a browser does by itself: send the cookie back. The value stays URL-encoded, exactly as it was received
        private async Task<HttpResponseMessage> SendWithCookieAsync(string url, string? refreshToken, bool csrfHeader)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, url);
            if (refreshToken is not null)
                message.Headers.Add("Cookie", $"refreshToken={refreshToken}");
            if (csrfHeader)
                message.Headers.Add("X-Requested-With", "XMLHttpRequest");

            return await _client.SendAsync(message);
        }

        // "refreshToken=<value>; expires=...; path=/api/auth; secure; samesite=strict; httponly" → <value>
        private static string ReadRefreshToken(HttpResponseMessage response)
        {
            var setCookie = response.Headers.GetValues("Set-Cookie").Single();
            return setCookie.Split(';')[0]["refreshToken=".Length..];
        }
    }
}
