using System.Net;
using System.Net.Http.Json;

namespace FlyReservation.IntegrationTests
{
    public class PassengersEndpointTests : IClassFixture<FlyReservationApiFactory>
    {
        private readonly HttpClient _client;

        public PassengersEndpointTests(FlyReservationApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        // Lost update: two admins open the same passenger, both save; the second must not silently undo the first
        [Fact]
        public async Task UpdatePassenger_TwoAdminsEditTheSameRead_SecondGets412AndFirstChangeIsKept()
        {
            var passenger = await _client.CreatePassengerAsync();
            var (firstCopy, firstETag) = await _client.GetPassengerForEditAsync(passenger.Id);
            var (secondCopy, secondETag) = await _client.GetPassengerForEditAsync(passenger.Id);
            var newEmail = $"{Guid.NewGuid()}@new.com";

            // Admin 1 changes the email
            var first = await _client.PutPassengerAsync(passenger.Id,
                firstCopy.ToUpdateRequest() with { Email = newEmail }, firstETag);

            // Admin 2 changes the last name; the form still shows the old email and the old version
            var second = await _client.PutPassengerAsync(passenger.Id,
                secondCopy.ToUpdateRequest() with { LastName = "Kaya" }, secondETag);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.PreconditionFailed, second.StatusCode);

            var after = await _client.GetPassengerAsync(passenger.Id);
            Assert.Equal(newEmail, after.Email);
            Assert.Equal(passenger.LastName, after.LastName);
        }

        [Fact]
        public async Task UpdatePassenger_WithoutIfMatch_Returns428AndChangesNothing()
        {
            var passenger = await _client.CreatePassengerAsync();
            var (copy, _) = await _client.GetPassengerForEditAsync(passenger.Id);

            var response = await _client.PutPassengerAsync(passenger.Id, copy.ToUpdateRequest() with { LastName = "Kaya" }, eTag: null);

            Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
            Assert.Equal(passenger.LastName, (await _client.GetPassengerAsync(passenger.Id)).LastName);
        }
    }
}
