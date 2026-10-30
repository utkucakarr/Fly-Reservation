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
        public async Task UpdateFlight_BelowSoldSeats_Returns409AndKeepsSeats()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 3);
            var firstPassenger = await _client.CreatePassengerAsync();
            var secondPassenger = await _client.CreatePassengerAsync();
            await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, firstPassenger.Id));
            await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, secondPassenger.Id));
            var update = new UpdateFlightRequest(
                flight.FlightNumber, flight.Origin, flight.Destination,
                flight.DepartureTime, flight.ArrivalTime, TotalSeats: 1, flight.Price);

            var response = await _client.PutAsJsonAsync($"/api/flights/{flight.Id}", update);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(3, flightAfter.TotalSeats);
            Assert.Equal(1, flightAfter.AvailableSeats);
        }
    }
}
