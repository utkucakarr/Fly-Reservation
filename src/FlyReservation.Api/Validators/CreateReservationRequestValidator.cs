using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    // A missing int arrives as 0. Without this, an empty body became 404 "Flight 0 not found."
    public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
    {
        public CreateReservationRequestValidator()
        {
            RuleFor(x => x.FlightId)
                .GreaterThan(0)
                .WithMessage("FlightId is required and must be greater than 0.");

            RuleFor(x => x.PassengerId)
                .GreaterThan(0)
                .WithMessage("PassengerId is required and must be greater than 0.");
        }
    }
}
