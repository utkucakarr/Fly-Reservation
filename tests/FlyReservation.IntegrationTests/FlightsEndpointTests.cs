using System.Net;
using System.Net.Http.Json;
using FlyReservation.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.IntegrationTests
{
    public class FlightsEndpointTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public FlightsEndpointTests(FlyReservationApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateFlight_WithInvalidData_Returns400WithFieldErrors()
        {
            var request = new CreateFlightRequest("", "IST", "IST", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-2), -50, -100);

            var response = await _client.PostAsJsonAsync("/api/flights", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.Contains("TotalSeats", problem!.Errors.Keys);
            Assert.Contains("Destination", problem.Errors.Keys);
        }

        [Fact]
        public async Task CreateFlight_SameNumberAndDepartureTwice_Returns409()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var duplicate = new CreateFlightRequest(
                flight.FlightNumber, flight.Origin, flight.Destination,
                flight.DepartureTime, flight.ArrivalTime, flight.TotalSeats, flight.Price);

            var response = await _client.PostAsJsonAsync("/api/flights", duplicate);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetFlights_PageSizeAboveLimit_Returns400()
        {
            var response = await _client.GetAsync("/api/flights?pageSize=101");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFlights_ReturnsRequestedPageSize()
        {
            await _client.CreateFlightAsync(totalSeats: 10);
            await _client.CreateFlightAsync(totalSeats: 10);
            await _client.CreateFlightAsync(totalSeats: 10);

            var page = await _client.GetFromJsonAsync<PagedResponse<FlightResponse>>("/api/flights?page=1&pageSize=2");

            Assert.Equal(2, page!.Items.Count);
            Assert.True(page.TotalCount >= 3);
        }

        [Fact]
        public async Task SearchFlights_ReturnsOnlyThatRouteAndDay_OrderedByDeparture()
        {
            // A route and day no other test uses, so the shared test database doesn't interfere
            var day = DateTime.UtcNow.Date.AddDays(400);
            var late = await CreateFlightOnRouteAsync("TZX", "GZT", day.AddHours(18));
            var early = await CreateFlightOnRouteAsync("TZX", "GZT", day.AddHours(7));
            await CreateFlightOnRouteAsync("TZX", "GZT", day.AddDays(1).AddHours(7));   // next day
            await CreateFlightOnRouteAsync("TZX", "ERZ", day.AddHours(9));              // other route

            var results = await _client.GetFromJsonAsync<List<FlightResponse>>(
                $"/api/flights/search?origin=TZX&destination=GZT&date={day:yyyy-MM-dd}");

            Assert.Equal(new[] { early.Id, late.Id }, results!.Select(f => f.Id));
        }

        [Fact]
        public async Task SearchFlights_AfterBooking_ShowsUpdatedSeats()
        {
            var day = DateTime.UtcNow.Date.AddDays(410);
            var flight = await CreateFlightOnRouteAsync("ERZ", "TZX", day.AddHours(10));
            var url = $"/api/flights/search?origin=ERZ&destination=TZX&date={day:yyyy-MM-dd}";
            await _client.GetFromJsonAsync<List<FlightResponse>>(url);   // the first search fills the cache
            var passenger = await _client.CreatePassengerAsync();

            var booking = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, passenger.Id));
            booking.EnsureSuccessStatusCode();

            var results = await _client.GetFromJsonAsync<List<FlightResponse>>(url);
            Assert.Equal(flight.TotalSeats - 1, results!.Single().AvailableSeats);
        }

        [Fact]
        public async Task SearchFlights_AfterCreatingFlight_IncludesNewFlight()
        {
            var day = DateTime.UtcNow.Date.AddDays(420);
            var url = $"/api/flights/search?origin=GZT&destination=ERZ&date={day:yyyy-MM-dd}";
            var before = await _client.GetFromJsonAsync<List<FlightResponse>>(url);   // empty result is cached too

            var flight = await CreateFlightOnRouteAsync("GZT", "ERZ", day.AddHours(12));

            var after = await _client.GetFromJsonAsync<List<FlightResponse>>(url);
            Assert.Empty(before!);
            Assert.Equal(flight.Id, after!.Single().Id);
        }

        private async Task<FlightResponse> CreateFlightOnRouteAsync(string origin, string destination, DateTime departure)
        {
            var request = new CreateFlightRequest(
                FlightNumber: $"SR{Random.Shared.Next(1, 9999)}",
                Origin: origin,
                Destination: destination,
                DepartureTime: departure,
                ArrivalTime: departure.AddHours(1),
                TotalSeats: 10,
                Price: 500);

            var response = await _client.PostAsJsonAsync("/api/flights", request);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<FlightResponse>())!;
        }
    }
}