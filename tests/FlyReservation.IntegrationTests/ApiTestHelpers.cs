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
                PassportNumber: TestPassportNumber);

            var response = await client.PostAsJsonAsync("/api/passengers", request);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<PassengerResponse>())!;
        }

        public static async Task<FlightResponse> GetFlightAsync(this HttpClient client, int id)
        {
            return (await client.GetFromJsonAsync<FlightResponse>($"/api/flights/{id}"))!;
        }

        public static async Task<PassengerResponse> GetPassengerAsync(this HttpClient client, int id)
        {
            return (await client.GetFromJsonAsync<PassengerResponse>($"/api/passengers/{id}"))!;
        }

        // The passport number CreatePassengerAsync uses. GET does not return it, but PUT needs it
        public const string TestPassportNumber = "U12345678";

        // What an admin's edit form sends back: every field as it was read, before any change
        public static UpdatePassengerRequest ToUpdateRequest(this PassengerResponse passenger)
        {
            return new UpdatePassengerRequest(passenger.FirstName, passenger.LastName, passenger.Email, TestPassportNumber);
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

        public static async Task ChangeFlightPriceAsync(this HttpClient client, FlightResponse flight, decimal price)
        {
            var (current, eTag) = await client.GetFlightForEditAsync(flight.Id);

            var response = await client.PutFlightAsync(flight.Id, current.ToUpdateRequest() with { Price = price }, eTag);
            response.EnsureSuccessStatusCode();
        }

        public static Task<(FlightResponse Flight, string ETag)> GetFlightForEditAsync(this HttpClient client, int id)
            => client.GetForEditAsync<FlightResponse>($"/api/flights/{id}");

        public static Task<HttpResponseMessage> PutFlightAsync(this HttpClient client, int id, UpdateFlightRequest request, string? eTag)
            => client.PutWithIfMatchAsync($"/api/flights/{id}", request, eTag);

        public static Task<(PassengerResponse Passenger, string ETag)> GetPassengerForEditAsync(this HttpClient client, int id)
            => client.GetForEditAsync<PassengerResponse>($"/api/passengers/{id}");

        public static Task<HttpResponseMessage> PutPassengerAsync(this HttpClient client, int id, UpdatePassengerRequest request, string? eTag)
            => client.PutWithIfMatchAsync($"/api/passengers/{id}", request, eTag);

        // What an admin's edit form loads: the record and the version (ETag) it was read at
        private static async Task<(T Record, string ETag)> GetForEditAsync<T>(this HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var record = (await response.Content.ReadFromJsonAsync<T>())!;
            return (record, response.Headers.ETag!.ToString());
        }

        // PUT with If-Match: "update only if the record is still at this version". Null sends no If-Match
        private static async Task<HttpResponseMessage> PutWithIfMatchAsync<T>(this HttpClient client, string url, T request, string? eTag)
        {
            var message = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(request)
            };
            if (eTag is not null)
                message.Headers.TryAddWithoutValidation("If-Match", eTag);

            return await client.SendAsync(message);
        }

        // What an admin's edit form sends back: every field as it was read, before any change
        public static UpdateFlightRequest ToUpdateRequest(this FlightResponse flight)
        {
            return new UpdateFlightRequest(flight.FlightNumber, flight.Origin, flight.Destination,
                flight.DepartureTime, flight.ArrivalTime, flight.Price);
        }
    }
}