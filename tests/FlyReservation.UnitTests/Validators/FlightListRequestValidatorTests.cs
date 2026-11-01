using FlyReservation.Api.Dtos;
using FlyReservation.Api.Validators;

namespace FlyReservation.UnitTests.Validators
{
    public class FlightListRequestValidatorTests
    {
        private readonly FlightListRequestValidator _validator = new();

        [Fact]
        public void Validate_DefaultRequest_HasNoErrors()
        {
            var result = _validator.Validate(new FlightListRequest());

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_PageBelowOne_FailsOnPage(int page)
        {
            var request = new FlightListRequest { Page = page };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(FlightListRequest.Page));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(1000000)]
        public void Validate_PageSizeOutOfRange_FailsOnPageSize(int pageSize)
        {
            var request = new FlightListRequest { PageSize = pageSize };

            var result = _validator.Validate(request);

            Assert.Contains(result.Errors, e => e.PropertyName == nameof(FlightListRequest.PageSize));
        }

        // Boundaries: 1 and 100 are the smallest and largest allowed page sizes
        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public void Validate_PageSizeOnBoundary_HasNoErrors(int pageSize)
        {
            var request = new FlightListRequest { PageSize = pageSize };

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }
    }
}
