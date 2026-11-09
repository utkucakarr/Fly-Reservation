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
        [InlineData("/api/Reservations", "post", new[] { "201", "400", "404", "409", "422" })]
        [InlineData("/api/Reservations/{id}", "delete", new[] { "204", "404", "409" })]
        [InlineData("/api/Reservations/{id}/payment", "post", new[] { "200", "404", "409", "503" })]
        [InlineData("/api/Flights", "post", new[] { "201", "400", "409" })]
        [InlineData("/api/Flights/{id}", "get", new[] { "200", "404" })]
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
