using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(100);

            // Length, not "an upper case letter, a digit and a symbol": those rules end in "Password1!"
            RuleFor(r => r.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        }
    }
}
