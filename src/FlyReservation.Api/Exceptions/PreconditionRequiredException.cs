namespace FlyReservation.Api.Exceptions
{
    // 428: the update did not say which version it was based on (no If-Match)
    public class PreconditionRequiredException : Exception
    {
        public PreconditionRequiredException(string message) : base(message)
        {
        }
    }
}
