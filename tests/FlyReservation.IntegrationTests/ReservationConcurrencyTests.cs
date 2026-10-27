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
    }
}
