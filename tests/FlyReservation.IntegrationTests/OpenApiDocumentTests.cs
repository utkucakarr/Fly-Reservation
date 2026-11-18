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
    }
}
