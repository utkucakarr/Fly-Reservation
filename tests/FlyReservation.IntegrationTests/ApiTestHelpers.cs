using System.Net.Http.Json;
using FlyReservation.Api.Dtos;

namespace FlyReservation.IntegrationTests
{
    // Creates test data through the API itself, with unique values so tests never collide
    public static class ApiTestHelpers
    {
        public static async Task<FlightResponse> CreateFlightAsync(this HttpClient client, int totalSeats)
        {
            var departure = DateTime.UtcNow.AddDays(Random.Shared.Next(10, 300)).AddMinutes(Random.Shared.Next(0, 1440));

            var request = new CreateFlightRequest(
                FlightNumber: $"IT{Random.Shared.Next(1, 9999)}",
                Origin: "IST",
                Destination: "AYT",
                DepartureTime: departure,
                ArrivalTime: departure.AddHours(1),
                TotalSeats: totalSeats,
                Price: 900);

            var response = await client.PostAsJsonAsync("/api/flights", request);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<FlightResponse>())!;
        }

        public static async Task<PassengerResponse> CreatePassengerAsync(this HttpClient client)
        {
            var request = new CreatePassengerRequest(
                FirstName: "Test",
                LastName: "Passenger",
                Email: $"{Guid.NewGuid()}@test.com",
                PassportNumber: "U12345678");

            var response = await client.PostAsJsonAsync("/api/passengers", request);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<PassengerResponse>())!;
        }

        public static async Task<FlightResponse> GetFlightAsync(this HttpClient client, int id)
        {
            return (await client.GetFromJsonAsync<FlightResponse>($"/api/flights/{id}"))!;
        }

        public static async Task<HttpResponseMessage> PostReservationAsync(
    this HttpClient client, CreateReservationRequest request, string idempotencyKey)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "/api/reservations")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("Idempotency-Key", idempotencyKey);

            return await client.SendAsync(message);
        }
    }
}