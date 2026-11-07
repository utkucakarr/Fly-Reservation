using System.Net;
using System.Net.Http.Json;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FlyReservation.IntegrationTests
{
    public class ReservationExpiryTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly FlyReservationApiFactory _factory;
        private readonly HttpClient _client;

        public ReservationExpiryTests(FlyReservationApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateReservation_StartsPendingWithADeadline()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);

            var reservation = await BookAsync(_client, flight.Id);

            Assert.Equal("PendingPayment", reservation.Status);
            Assert.True(reservation.PaymentDueAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task Expire_WhenDeadlinePassed_MarksExpiredAndReleasesSeat()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(_client, flight.Id);
            await _factory.MakeOverdueAsync(reservation.Id);

            await _factory.RunExpiryAsync();

            Assert.Equal("Expired", (await GetReservationAsync(reservation.Id)).Status);
            Assert.Equal(1, (await _client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        [Fact]
        public async Task Expire_WhenDeadlineNotPassed_KeepsTheSeat()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(_client, flight.Id);

            await _factory.RunExpiryAsync();

            Assert.Equal("PendingPayment", (await GetReservationAsync(reservation.Id)).Status);
            Assert.Equal(0, (await _client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        [Fact]
        public async Task Expire_WhenSeveralRunnersRace_ReleasesTheSeatOnce()
        {
            // 2 seats: a double release would show as 2 free seats instead of hitting the check constraint
            var flight = await _client.CreateFlightAsync(totalSeats: 2);
            var overdue = await BookAsync(_client, flight.Id);
            await BookAsync(_client, flight.Id);
            await _factory.MakeOverdueAsync(overdue.Id);

            await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _factory.RunExpiryAsync()));

            Assert.Equal(1, (await _client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        [Fact]
        public async Task CreateReservation_AfterOwnReservationExpired_Returns201()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 1);
            var passenger = await _client.CreatePassengerAsync();
            var request = new CreateReservationRequest(flight.Id, passenger.Id);
            var first = await (await _client.PostAsJsonAsync("/api/reservations", request)).Content.ReadFromJsonAsync<ReservationResponse>();
            await _factory.MakeOverdueAsync(first!.Id);
            await _factory.RunExpiryAsync();

            var response = await _client.PostAsJsonAsync("/api/reservations", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CancelReservation_AfterExpired_Returns409AndDoesNotReleaseTwice()
        {
            var flight = await _client.CreateFlightAsync(totalSeats: 2);
            var overdue = await BookAsync(_client, flight.Id);
            await BookAsync(_client, flight.Id);
            await _factory.MakeOverdueAsync(overdue.Id);
            await _factory.RunExpiryAsync();

            var response = await _client.DeleteAsync($"/api/reservations/{overdue.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(1, (await _client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        [Fact]
        public async Task Pay_ConfirmsTheReservation_AndTheJobLeavesItAlone()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.OK);
            var client = CreateClientWith(provider);
            var flight = await client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(client, flight.Id);

            var response = await client.PostAsync($"/api/reservations/{reservation.Id}/payment", null);
            await _factory.RunExpiryAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var after = await GetReservationAsync(reservation.Id);
            Assert.Equal("Confirmed", after.Status);
            Assert.Null(after.PaymentDueAt);
            Assert.Equal(0, (await client.GetFlightAsync(flight.Id)).AvailableSeats);
        }

        [Fact]
        public async Task Pay_Twice_ChargesOnce()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.OK);
            var client = CreateClientWith(provider);
            var flight = await client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(client, flight.Id);

            await client.PostAsync($"/api/reservations/{reservation.Id}/payment", null);
            var second = await client.PostAsync($"/api/reservations/{reservation.Id}/payment", null);

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var payment = await second.Content.ReadFromJsonAsync<ReservationPaymentResponse>();
            Assert.Equal("tx_test", payment!.TransactionId);
            Assert.Equal(1, provider.Calls);
        }

        [Fact]
        public async Task Pay_AfterDeadline_Returns409WithoutCharging()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.OK);
            var client = CreateClientWith(provider);
            var flight = await client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(client, flight.Id);
            await _factory.MakeOverdueAsync(reservation.Id);

            var response = await client.PostAsync($"/api/reservations/{reservation.Id}/payment", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(0, provider.Calls);
        }

        [Fact]
        public async Task Pay_WhenProviderIsDown_KeepsTheOriginalDeadline()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.ServiceUnavailable);
            var client = CreateClientWith(provider);
            var flight = await client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(client, flight.Id);

            await client.PostAsync($"/api/reservations/{reservation.Id}/payment", null);

            var after = await GetReservationAsync(reservation.Id);
            Assert.Equal("PendingPayment", after.Status);
            Assert.Equal(reservation.PaymentDueAt!.Value, after.PaymentDueAt!.Value, TimeSpan.FromSeconds(1));   // the claim must not shorten 15 minutes to 1
        }

        [Fact]
        public async Task Worker_ExpiresReservationsWithoutAnyRequest()
        {
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Reservations:PaymentWindow", "00:00:01");
                builder.UseSetting("Reservations:ExpiryCheckInterval", "00:00:00.200");
            }).CreateClient();
            var flight = await client.CreateFlightAsync(totalSeats: 1);
            var reservation = await BookAsync(client, flight.Id);

            var seatsLeft = 0;
            for (var i = 0; i < 50 && seatsLeft == 0; i++)
            {
                await Task.Delay(200);
                seatsLeft = (await client.GetFlightAsync(flight.Id)).AvailableSeats;
            }

            Assert.Equal(1, seatsLeft);
            Assert.Equal("Expired", (await GetReservationAsync(reservation.Id)).Status);
        }

        private HttpClient CreateClientWith(FakePaymentHandler provider)
        {
            return _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                    services.AddHttpClient<IPaymentClient, PaymentClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => provider)))
                .CreateClient();
        }

        private static async Task<ReservationResponse> BookAsync(HttpClient client, int flightId)
        {
            var passenger = await client.CreatePassengerAsync();

            var response = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flightId, passenger.Id));
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        }

        private async Task<ReservationResponse> GetReservationAsync(int id)
        {
            return (await _client.GetFromJsonAsync<ReservationResponse>($"/api/reservations/{id}"))!;
        }
    }
}
