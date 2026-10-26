using FlyReservation.Api.Dtos;
using FlyReservation.Api.Validators;

namespace FlyReservation.UnitTests.Validators
{
    public class CreateFlightRequestValidatorTests
    {
        private readonly CreateFlightRequestValidator _validator = new();

        // Dates are relative to now: the validator rejects past departures,
        // so a fixed date would make this test fail once that date has passed
        private static CreateFlightRequest ValidRequest()
        {
            var departure = DateTime.UtcNow.AddDays(30);

            return new CreateFlightRequest(
                FlightNumber: "TK2024",
                Origin: "IST",
                Destination: "ESB",
                DepartureTime: departure,
                ArrivalTime: departure.AddHours(1),
                TotalSeats: 180,
                Price: 1500);
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

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFlightRequest.Destination));
        }

        [Fact]
        public void Validate_DepartureInPast_FailsOnDepartureTime()
        {
            var request = ValidRequest() with { DepartureTime = DateTime.UtcNow.AddDays(-1) };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFlightRequest.DepartureTime));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        [InlineData(1001)]
        public void Validate_TotalSeatsOutOfRange_FailsOnTotalSeats(int totalSeats)
        {
            var request = ValidRequest() with { TotalSeats = totalSeats };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFlightRequest.TotalSeats));
        }

        [Theory]
        [InlineData("")]
        [InlineData("TK")]
        [InlineData("tk2024")]
        [InlineData("TK20245")]
        public void Validate_InvalidFlightNumber_FailsOnFlightNumber(string flightNumber)
        {
            var request = ValidRequest() with { FlightNumber = flightNumber };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFlightRequest.FlightNumber));
        }
    }
}
