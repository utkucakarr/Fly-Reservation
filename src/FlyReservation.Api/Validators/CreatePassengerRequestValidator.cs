using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    public class CreatePassengerRequestValidator : AbstractValidator<CreatePassengerRequest>
    {
        public CreatePassengerRequestValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
            RuleFor(x => x.PassportNumber).NotEmpty().MaximumLength(20);
        }
    }
}
