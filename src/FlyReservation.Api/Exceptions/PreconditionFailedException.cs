namespace FlyReservation.Api.Exceptions
{
    // 412: the version the client read (If-Match) is no longer the current one
    public class PreconditionFailedException : Exception
    {
        public PreconditionFailedException(string message) : base(message)
        {
        }
    }
}
