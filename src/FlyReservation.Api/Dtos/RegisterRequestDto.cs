namespace FlyReservation.Api.Dtos
{
    // No Role on purpose: a new account is always a customer. Otherwise anyone could send "role": "Admin"
    public record RegisterRequest(string Email, string Password);
}
