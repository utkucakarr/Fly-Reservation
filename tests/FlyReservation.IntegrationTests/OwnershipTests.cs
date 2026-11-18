using System.Net;
using System.Net.Http.Json;
using FlyReservation.Api.Dtos;

namespace FlyReservation.IntegrationTests
{
    public class OwnershipTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly FlyReservationApiFactory _factory;
        private readonly HttpClient _client;

        public OwnershipTests(FlyReservationApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();   // no token: a stranger
        }

        // Ids are 1, 2, 3...: anyone can guess one. Knowing an id must not be enough to cancel someone's booking
        [Fact]
        public async Task CancelReservation_ByStrangerWithoutToken_Returns401AndSeatStaysTaken()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var alice = await _factory.CreateCustomerClientAsync();
            var reservation = await alice.CreateReservationAsync(flight.Id);

            var response = await _client.DeleteAsync($"/api/reservations/{reservation.Id}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(9, flightAfter.AvailableSeats);
        }

        // Both are logged-in customers. Being logged in is not the same as owning the booking
        [Fact]
        public async Task CancelReservation_ByAnotherCustomer_Returns404AndSeatStaysTaken()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var alice = await _factory.CreateCustomerClientAsync();
            var bob = await _factory.CreateCustomerClientAsync();
            var reservation = await alice.CreateReservationAsync(flight.Id);

            var response = await bob.DeleteAsync($"/api/reservations/{reservation.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var flightAfter = await _client.GetFlightAsync(flight.Id);
            Assert.Equal(9, flightAfter.AvailableSeats);
        }

        // Everything else Bob could try with the ids of Alice's booking: each must look like it does not exist
        [Theory]
        [InlineData("GET", "/api/reservations/{reservation}")]
        [InlineData("POST", "/api/reservations/{reservation}/payment")]
        [InlineData("GET", "/api/passengers/{passenger}")]
        public async Task AnotherCustomer_GetsNotFoundForSomeoneElsesRecords(string method, string url)
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var alice = await _factory.CreateCustomerClientAsync();
            var bob = await _factory.CreateCustomerClientAsync();
            var reservation = await alice.CreateReservationAsync(flight.Id);

            var response = await bob.SendAsync(new HttpRequestMessage(new HttpMethod(method), url
                .Replace("{reservation}", reservation.Id.ToString())
                .Replace("{passenger}", reservation.PassengerId.ToString())));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Bob sends a full, valid edit with a made-up ETag: the passenger must stay as Alice saved them
        [Fact]
        public async Task UpdatePassenger_ByAnotherCustomer_Returns404AndChangesNothing()
        {
            var alice = await _factory.CreateCustomerClientAsync();
            var bob = await _factory.CreateCustomerClientAsync();
            var passenger = await alice.CreatePassengerAsync();

            var response = await bob.PutPassengerAsync(passenger.Id, passenger.ToUpdateRequest() with { FirstName = "Bob" }, "\"0\"");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(passenger.FirstName, (await alice.GetPassengerAsync(passenger.Id)).FirstName);
        }

        // Bob uses Alice's passenger id in his own booking request
        [Fact]
        public async Task CreateReservation_ForSomeoneElsesPassenger_Returns404AndTakesNoSeat()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var alice = await _factory.CreateCustomerClientAsync();
            var bob = await _factory.CreateCustomerClientAsync();
            var alicesPassenger = await alice.CreatePassengerAsync();

            var response = await bob.PostReservationAsync(
                new CreateReservationRequest(flight.Id, alicesPassenger.Id), Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(10, (await _client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        // The owner check must not lock out the admin (support cancels a booking for a customer)
        [Fact]
        public async Task CancelReservation_ByAdmin_Returns204AndReleasesSeat()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var alice = await _factory.CreateCustomerClientAsync();
            var reservation = await alice.CreateReservationAsync(flight.Id);

            var response = await _client.SendAsAdminAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/reservations/{reservation.Id}"));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(10, (await _client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        // "Mine" lists only the caller's records, never another customer's
        [Fact]
        public async Task GetMine_ReturnsOnlyTheCallersPassengersAndReservations()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 10);
            var alice = await _factory.CreateCustomerClientAsync();
            var bob = await _factory.CreateCustomerClientAsync();
            var first = await alice.CreateReservationAsync(flight.Id);
            var second = await alice.CreateReservationAsync(flight.Id);
            await bob.CreateReservationAsync(flight.Id);

            var reservations = await alice.GetFromJsonAsync<List<ReservationResponse>>("/api/reservations/mine");
            var passengers = await alice.GetFromJsonAsync<List<PassengerResponse>>("/api/passengers/mine");

            Assert.Equal(new[] { first.Id, second.Id }.Order(), reservations!.Select(r => r.Id).Order());
            Assert.Equal(new[] { first.PassengerId, second.PassengerId }.Order(), passengers!.Select(p => p.Id).Order());
        }
    }
}
