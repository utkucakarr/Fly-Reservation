namespace FlyReservation.Api.Exceptions
{
    // Thrown by services when a requested entity does not exist.
    // GlobalExceptionHandler maps it to 404, so services stay unaware of HTTP.
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
