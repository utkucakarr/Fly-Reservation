using FlyReservation.Api.Dtos;
using FlyReservation.Api.Validators;

namespace FlyReservation.UnitTests.Validators
{
    public class CreateReservationRequestValidatorTests
    {
        private readonly CreateReservationRequestValidator _validator = new();

        [Fact]
        public void Validate_ValidRequest_HasNoErrors()
        {
            var result = _validator.Validate(new CreateReservationRequest(FlightId: 1, PassengerId: 1));

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidFlightId_FailsOnFlightId(int flightId)
        {
            var result = _validator.Validate(new CreateReservationRequest(flightId, PassengerId: 1));

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservationRequest.FlightId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidPassengerId_FailsOnPassengerId(int passengerId)
        {
            var result = _validator.Validate(new CreateReservationRequest(FlightId: 1, passengerId));

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReservationRequest.PassengerId));
        }
    }
}
