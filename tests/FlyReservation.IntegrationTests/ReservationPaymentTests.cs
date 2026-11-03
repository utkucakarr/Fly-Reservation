using System.Net;
using System.Net.Http.Json;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Payments;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FlyReservation.IntegrationTests
{
    public class ReservationPaymentTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly FlyReservationApiFactory _factory;

        public ReservationPaymentTests(FlyReservationApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Pay_WhenProviderFailsOnceThenSucceeds_Returns200()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
            var client = CreateClientWith(provider);
            var reservationId = await CreateReservationAsync(client);

            var response = await client.PostAsync($"/api/reservations/{reservationId}/payment", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, provider.Calls);   // the first attempt failed, the retry succeeded
        }

        [Fact]
        public async Task Pay_WhenProviderKeepsFailing_Returns503AfterRetries()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.ServiceUnavailable);
            var client = CreateClientWith(provider);
            var reservationId = await CreateReservationAsync(client);

            var response = await client.PostAsync($"/api/reservations/{reservationId}/payment", null);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(4, provider.Calls);   // 1 attempt + 3 retries
        }

        [Fact]
        public async Task Pay_RetriesSendTheSameIdempotencyKey()
        {
            var provider = new FakePaymentHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
            var client = CreateClientWith(provider);
            var reservationId = await CreateReservationAsync(client);

            await client.PostAsync($"/api/reservations/{reservationId}/payment", null);

            Assert.Equal(3, provider.IdempotencyKeys.Count);
            Assert.Single(provider.IdempotencyKeys.Distinct());
            Assert.NotNull(provider.IdempotencyKeys.First());
        }

        // The real API, but the payment provider's HTTP calls go to the fake handler.
        // The resilience handler from Program.cs stays in the pipeline, so retries really happen
        private HttpClient CreateClientWith(FakePaymentHandler provider)
        {
            return _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                    services.AddHttpClient<IPaymentClient, PaymentClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => provider)))
                .CreateClient();
        }

        private static async Task<int> CreateReservationAsync(HttpClient client)
        {
            var flight = await client.CreateFlightAsync(totalSeats: 10);
            var passenger = await client.CreatePassengerAsync();

            var response = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(flight.Id, passenger.Id));
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ReservationResponse>())!.Id;
        }
    }
}
