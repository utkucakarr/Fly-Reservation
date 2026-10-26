namespace FlyReservation.Api.Dtos
{
    public record UpdatePassengerRequest(string FirstName, string LastName, string Email, string PassportNumber);
}
