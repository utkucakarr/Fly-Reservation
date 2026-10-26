using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    public class CreateFlightRequestValidator : AbstractValidator<CreateFlightRequest>
    {
        public CreateFlightRequestValidator()
        {
            RuleFor(x => x.FlightNumber)
               .NotEmpty()
               .Matches(@"^[A-Z0-9]{2}\d{1,4}$")
               .WithMessage("Flight number must be a 2-character airline code followed by 1-4 digits (e.g. TK2024).");

            RuleFor(x => x.Origin)
                .NotEmpty()
                .Matches("^[A-Z]{3}$")
                .WithMessage("Origin must be a 3-letter uppercase airport code (e.g. IST).");

            RuleFor(x => x.Destination)
                .NotEmpty()
                .Matches("^[A-Z]{3}$")
                .WithMessage("Destination must be a 3-letter uppercase airport code (e.g. ESB).")
                .NotEqual(x => x.Origin)
                .WithMessage("Destination must be different from origin.");

            RuleFor(x => x.DepartureTime)
                .GreaterThan(_ => DateTime.UtcNow)
                .WithMessage("Departure time must be in the future.");

            RuleFor(x => x.ArrivalTime)
                .GreaterThan(x => x.DepartureTime)
                .WithMessage("Arrival time must be after departure time.");

            RuleFor(x => x.TotalSeats)
                .InclusiveBetween(1, 1000);

            RuleFor(x => x.Price)
                .GreaterThan(0);
        }
    }
}
