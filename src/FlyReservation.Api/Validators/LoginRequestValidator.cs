using FluentValidation;
using FlyReservation.Api.Dtos;

namespace FlyReservation.Api.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(r => r.Email).NotEmpty();
            RuleFor(r => r.Password).NotEmpty();
        }
    }
}
