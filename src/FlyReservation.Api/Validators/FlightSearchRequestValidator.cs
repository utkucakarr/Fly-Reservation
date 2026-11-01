using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    public class FlightSearchRequestValidator : AbstractValidator<FlightSearchRequest>
    {
        public FlightSearchRequestValidator()
        {
            RuleFor(x => x.Origin).NotEmpty().Matches("^[A-Z]{3}$");
            RuleFor(x => x.Destination).NotEmpty().Matches("^[A-Z]{3}$").NotEqual(x => x.Origin);
            RuleFor(x => x.Date).NotEmpty();
        }
    }
}
