using System.Text.Json;

namespace FlyReservation.IntegrationTests
{
    // The OpenAPI document must list the status codes each endpoint really returns.
    // OpenAPI only sees the method signature, not the exceptions the services throw
    public class OpenApiDocumentTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public OpenApiDocumentTests(FlyReservationApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("/api/Reservations", "post", new[] { "201", "400", "401", "404", "409", "422" })]
        [InlineData("/api/Reservations/{id}", "delete", new[] { "204", "401", "404", "409" })]
        [InlineData("/api/Reservations/{id}/payment", "post", new[] { "200", "401", "404", "409", "503" })]
        [InlineData("/api/Auth/login", "post", new[] { "200", "400", "401" })]
        [InlineData("/api/Auth/refresh", "post", new[] { "200", "400", "401" })]
        [InlineData("/api/Auth/logout", "post", new[] { "204", "400" })]
        [InlineData("/api/Passengers", "get", new[] { "200", "401", "403" })]
        [InlineData("/api/Passengers/{id}", "delete", new[] { "204", "401", "403", "404", "409" })]
        [InlineData("/api/Reservations", "get", new[] { "200", "401", "403" })]
        [InlineData("/api/Flights", "post", new[] { "201", "400", "401", "403", "409" })]
        [InlineData("/api/Flights/{id}", "get", new[] { "200", "404" })]
        [InlineData("/api/Flights/{id}", "put", new[] { "204", "400", "401", "403", "404", "409", "412", "428" })]
        [InlineData("/api/Passengers/{id}", "put", new[] { "204", "400", "401", "404", "409", "412", "428" })]
        [InlineData("/api/Auth/register", "post", new[] { "200", "400", "409" })]
        [InlineData("/api/Passengers/mine", "get", new[] { "200", "401" })]
        [InlineData("/api/Reservations/mine", "get", new[] { "200", "401" })]
        public async Task OpenApi_ListsTheStatusCodesTheEndpointReturns(string path, string method, string[] expected)
        {
            using var document = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json"));

            var documented = document.RootElement
                .GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("responses")
                .EnumerateObject()
                .Select(response => response.Name)
                .Order()
                .ToArray();

            Assert.Equal(expected.Order().ToArray(), documented);
        }

        // Scalar shows an "Authentication" box only if the document says how the API authenticates.
        // Only endpoints that need a token should ask for one
        [Fact]
        public async Task OpenApi_DescribesTheBearerTokenOnProtectedEndpointsOnly()
        {
            using var document = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json"));
            var root = document.RootElement;

            var hasScheme = root.TryGetProperty("components", out var components)
                && components.TryGetProperty("securitySchemes", out var schemes)
                && schemes.TryGetProperty("Bearer", out _);
            Assert.True(hasScheme, "The OpenAPI document has no Bearer security scheme");

            var paths = root.GetProperty("paths");
            Assert.True(paths.GetProperty("/api/Reservations/{id}").GetProperty("delete").TryGetProperty("security", out _));
            Assert.True(paths.GetProperty("/api/Flights").GetProperty("post").TryGetProperty("security", out _));
            Assert.False(paths.GetProperty("/api/Flights/{id}").GetProperty("get").TryGetProperty("security", out _));
            Assert.False(paths.GetProperty("/api/Auth/login").GetProperty("post").TryGetProperty("security", out _));
        }
    }
}
