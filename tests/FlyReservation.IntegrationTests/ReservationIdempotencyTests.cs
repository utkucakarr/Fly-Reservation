using FlyReservation.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace FlyReservation.IntegrationTests
{
    public class ReservationIdempotencyTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public ReservationIdempotencyTests(FlyReservationApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        // F1: the response was lost, the client retries with the same key
        [Fact]
        public async Task CreateReservation_SameKeyTwice_ReturnsSameReservationAndTakesOneSeat()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 5);
            var passenger = await _client.CreatePassengerAsync();
            var request = new CreateReservationRequest(flight.Id, passenger.Id);
            var key = Guid.NewGuid().ToString();

            var first = await _client.PostReservationAsync(request, key);
            var retry = await _client.PostReservationAsync(request, key);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
            var firstReservation = await first.Content.ReadFromJsonAsync<ReservationResponse>();
            var retryReservation = await retry.Content.ReadFromJsonAsync<ReservationResponse>();
            Assert.Equal(firstReservation!.PnrCode, retryReservation!.PnrCode);

            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(4, flightAfter.AvailableSeats);
        }

        // F2: retries arrive while the first request is still running, on the last seat.
        // The race window is only a few milliseconds: one round at a time never hit it (0 of 300 rounds).
        // Load widens it, so 20 rounds run at the same time, 10 times. Without the fix this failed in 10 of 10 runs
        [Fact]
        public async Task CreateReservation_SameKeyConcurrently_EveryRetryGetsTheSameReservation()
        {
            for (var batch = 0; batch < 10; batch++)
                await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => SameKeyRoundAsync()));
        }

        private async Task SameKeyRoundAsync()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var passenger = await _client.CreatePassengerAsync();
            var request = new CreateReservationRequest(flight.Id, passenger.Id);
            var key = Guid.NewGuid().ToString();

            var responses = await Task.WhenAll(Enumerable.Range(0, 10)
                .Select(_ => _client.PostReservationAsync(request, key)));

            foreach (var response in responses)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.True(response.StatusCode == HttpStatusCode.Created,
                    $"Expected 201, got {(int)response.StatusCode}: {body}");
            }
            var reservations = await Task.WhenAll(responses
                .Select(r => r.Content.ReadFromJsonAsync<ReservationResponse>()));
            Assert.Single(reservations.Select(r => r!.PnrCode).Distinct());

            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(0, flightAfter.AvailableSeats);
        }

        // F3: the client reused a key for a different booking by mistake
        [Fact]
        public async Task CreateReservation_SameKeyDifferentFlight_Returns422AndTakesNoSeat()
        {
            var firstFlight = await _client.CreateFlightAsync(totalSeats: 5);
            var secondFlight = await _client.CreateFlightAsync(totalSeats: 5);
            var passenger = await _client.CreatePassengerAsync();
            var key = Guid.NewGuid().ToString();
            await _client.PostReservationAsync(new CreateReservationRequest(firstFlight.Id, passenger.Id), key);

            var response = await _client.PostReservationAsync(new CreateReservationRequest(secondFlight.Id, passenger.Id), key);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var secondFlightAfter = await _client.GetFlightAsync(secondFlight.Id);
            Assert.Equal(5, secondFlightAfter.AvailableSeats);
        }
    }
}
