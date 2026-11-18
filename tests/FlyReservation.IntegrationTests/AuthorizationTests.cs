using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FlyReservation.IntegrationTests
{
    public class AuthorizationTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly FlyReservationApiFactory _factory;
        private readonly HttpClient _client;

        public AuthorizationTests(FlyReservationApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // Nobody logged in, no token: a stranger must not be able to delete a flight
        [Fact]
        public async Task DeleteFlight_WithoutToken_Returns401AndFlightStays()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);

            var response = await _client.DeleteAsync($"/api/flights/{flight.Id}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var stillThere = await _client.GetAsync($"/api/flights/{flight.Id}");
            Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
        }

        [Theory]
        [InlineData("POST", "/api/flights")]
        [InlineData("PUT", "/api/flights/1")]
        [InlineData("DELETE", "/api/flights/1")]
        [InlineData("GET", "/api/passengers")]
        [InlineData("DELETE", "/api/passengers/1")]
        [InlineData("GET", "/api/reservations")]
        public async Task AdminEndpoints_WithoutToken_Return401(string method, string url)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal(401, problem!.Status);
        }

        // 401 = "I don't know who you are". 403 = "I know who you are, and you may not do this"
        [Fact]
        public async Task DeleteFlight_WithValidTokenButNotAdmin_Returns403()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var tokenGenerator = _factory.Services.GetRequiredService<JwtTokenGenerator>();
            var (token, _) = tokenGenerator.Create(new User { Id = 999, Email = "someone@test.com", Role = "Customer" });

            var response = await DeleteFlightAsync(flight.Id, token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFlight_AsAdmin_Returns204()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);

            var response = await DeleteFlightAsync(flight.Id, await _client.GetAdminTokenAsync());

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // The payload is only Base64: anyone can decode it, change "role" and encode it again.
        // The signature was made over the original payload, so the changed token must be rejected
        [Fact]
        public async Task DeleteFlight_WithTamperedToken_Returns401()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var tokenGenerator = _factory.Services.GetRequiredService<JwtTokenGenerator>();
            var (customerToken, _) = tokenGenerator.Create(new User { Id = 999, Email = "someone@test.com", Role = "Customer" });
            var (adminToken, _) = tokenGenerator.Create(new User { Id = 999, Email = "someone@test.com", Role = "Admin" });
            // header.payload.signature: the admin payload with the signature of the customer token
            var forged = string.Join('.', adminToken.Split('.')[0], adminToken.Split('.')[1], customerToken.Split('.')[2]);

            var response = await DeleteFlightAsync(flight.Id, forged);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(ApiTestHelpers.AdminEmail, "wrong-password"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Same answer as a wrong password: the response must not reveal which emails have an account
        [Fact]
        public async Task Login_WithUnknownEmail_Returns401WithTheSameMessage()
        {
            var wrongPassword = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest(ApiTestHelpers.AdminEmail, "wrong-password"));
            var unknownEmail = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest("nobody@test.com", "wrong-password"));

            Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
            var first = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>();
            var second = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal(first!.Title, second!.Title);
        }

        // Since Adım 23c a customer needs an account: without a token nobody can create, read, change,
        // book, pay or cancel. (The admin-only endpoints are in AdminEndpoints_WithoutToken_Return401)
        [Theory]
        [InlineData("POST", "/api/passengers")]
        [InlineData("GET", "/api/passengers/mine")]
        [InlineData("GET", "/api/passengers/1")]
        [InlineData("PUT", "/api/passengers/1")]
        [InlineData("POST", "/api/reservations")]
        [InlineData("GET", "/api/reservations/mine")]
        [InlineData("GET", "/api/reservations/1")]
        [InlineData("DELETE", "/api/reservations/1")]
        [InlineData("POST", "/api/reservations/1/payment")]
        public async Task CustomerEndpoints_WithoutToken_Return401(string method, string url)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<HttpResponseMessage> DeleteFlightAsync(int id, string token)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/flights/{id}");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await _client.SendAsync(message);
        }
    }
}
