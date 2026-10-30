using FlyReservation.Api.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace FlyReservation.IntegrationTests
{
    public class ReservationConcurrencyTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public ReservationConcurrencyTests(FlyReservationApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateReservation_WhenManyBuyersRaceForLastSeat_OnlyOneSucceeds()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var passenger = await _client.CreatePassengerAsync();
            var request = new CreateReservationRequest(flight.Id, passenger.Id);

            // Send 50 requests at the same time instead of awaiting them one by one
            var tasks = Enumerable.Range(0, 50)
                .Select(_ => _client.PostAsJsonAsync("/api/reservations", request));
            var responses = await Task.WhenAll(tasks);

            var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
            Assert.Equal(1, created);
        }

        [Fact]
        public async Task CreateReservation_WhenSeatsAreEnoughForEveryone_EveryBuyerSucceeds()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 50);   // ya da 50
            var passengers = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => _client.CreatePassengerAsync()));

            // 50 different people send their request at the same time
            var tasks = passengers
                .Select(p => _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, p.Id)));
            var responses = await Task.WhenAll(tasks);

            var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
            Assert.Equal(50, created);
        }

        [Fact]
        public async Task CreateReservation_WhenSamePassengerDoubleClicks_OnlyOneSucceedsAndOneSeatIsTaken()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 5);
            var passanger = await _client.CreatePassengerAsync();
            var request = new CreateReservationRequest(flight.Id, passanger.Id);

            // Both clicks arrive at the same moment, so both pass the "already booked?" check
            var responses = await Task.WhenAll(
                _client.PostAsJsonAsync("/api/reservations", request),
                _client.PostAsJsonAsync("/api/reservations", request)
                );

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(4, flightAfter.AvailableSeats);
        }
    }
}
