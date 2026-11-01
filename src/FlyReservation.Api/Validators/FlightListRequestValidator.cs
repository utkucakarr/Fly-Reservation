using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    public class FlightListRequestValidator : AbstractValidator<FlightListRequest>
    {
        public FlightListRequestValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

            // Without an upper limit, pageSize=1000000 would bring back the original problem
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
