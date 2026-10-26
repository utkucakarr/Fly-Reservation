namespace FlyReservation.Api.Dtos
{
    public record CreatePassengerRequest(string FirstName, string LastName, string Email, string PassportNumber);
}
