namespace FlyReservation.Api.Exceptions
{
    public class InvalidRefreshTokenException : Exception
    {
        // One message for unknown, expired, used and revoked tokens: the caller only needs to know "log in again"
        public InvalidRefreshTokenException() : base("The refresh token is invalid or expired. Log in again.")
        {
        }
    }
}
