using FlyReservation.Api.Dtos;
using FlyReservation.Api.Validators;

namespace FlyReservation.UnitTests.Validators
{
    public class FlightSearchRequestValidatorTests
    {
        private readonly FlightSearchRequestValidator _validator = new();

        private static FlightSearchRequest ValidRequest()
        {
            return new FlightSearchRequest
            {
                Origin = "IST",
                Destination = "AYT",
                Date = new DateOnly(2026, 12, 15)
            };
        }

        [Fact]
        public void Validate_ValidRequest_HasNoErrors()
        {
            var result = _validator.Validate(ValidRequest());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_SameOriginAndDestination_FailsOnDestination()
        {
            var request = ValidRequest() with { Destination = "IST" };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(FlightSearchRequest.Destination));
        }

        [Theory]
        [InlineData("")]
        [InlineData("ist")]
        [InlineData("ISTA")]
        public void Validate_InvalidOrigin_FailsOnOrigin(string origin)
        {
            var request = ValidRequest() with { Origin = origin };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(FlightSearchRequest.Origin));
        }

        // A missing ?date= in the query string binds to default(DateOnly), which must be rejected
        [Fact]
        public void Validate_MissingDate_FailsOnDate()
        {
            var request = ValidRequest() with { Date = default };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(FlightSearchRequest.Date));
        }
    }
}
