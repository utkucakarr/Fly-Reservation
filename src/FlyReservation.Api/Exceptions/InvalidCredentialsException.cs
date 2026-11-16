namespace FlyReservation.Api.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        // One message for "no such user" and "wrong password": the response must not reveal which emails exist
        public InvalidCredentialsException() : base("Email or password is incorrect.")
        {
        }
    }
}
