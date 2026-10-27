using System.Net;
using System.Net.Http.Json;
using FlyReservation.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.IntegrationTests
{
    public class ReservationsEndpointTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public ReservationsEndpointTests(FlyReservationApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateReservation_WhenSeatsAvailable_Returns201AndDecrementsSeats()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 2);
            var passenger = await _client.CreatePassengerAsync();

            var response = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, passenger.Id));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>();
            Assert.Equal(flight.Id, reservation!.FlightId);
            Assert.Matches("^[A-Z0-9]{6}$", reservation.PnrCode);

            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(1, flightAfter.AvailableSeats);
        }

        [Fact]
        public async Task CreateReservation_WhenFullyBooked_Returns409WithProblemDetails()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var passenger = await _client.CreatePassengerAsync();
            var request = new CreateReservationRequest(flight.Id, passenger.Id);
            await _client.PostAsJsonAsync("/api/reservations", request);

            var response = await _client.PostAsJsonAsync("/api/reservations", request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal($"Flight {flight.FlightNumber} has no available seats.", problem!.Title);
            Assert.True(problem.Extensions.ContainsKey("traceId"));

            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(0, flightAfter.AvailableSeats);
        }

        [Fact]
        public async Task DeleteReservation_ReleasesSeat()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var passenger = await _client.CreatePassengerAsync();
            var created = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, passenger.Id));
            var reservation = await created.Content.ReadFromJsonAsync<ReservationResponse>();

            var response = await _client.DeleteAsync($"/api/reservations/{reservation!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(1, flightAfter.AvailableSeats);
        }

        [Fact]
        public async Task CreateReservation_ForUnknownFlight_Returns404()
        {
            var passenger = await _client.CreatePassengerAsync();

            var response = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(999999, passenger.Id));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
