namespace FlyReservation.Api.Exceptions
{
    public class IdempotencyKeyReusedException : Exception
    {
        public IdempotencyKeyReusedException(string message) : base(message)
        {
            
        }
    }
}
