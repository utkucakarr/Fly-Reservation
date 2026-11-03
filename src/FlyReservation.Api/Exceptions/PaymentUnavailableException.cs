namespace FlyReservation.Api.Exceptions
{
    public class PaymentUnavailableException : Exception
    {
        public PaymentUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
