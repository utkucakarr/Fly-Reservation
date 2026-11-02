namespace FlyReservation.Api.Payments
{
    public interface IPaymentClient
    {
        // Returns the provider's transaction id
        Task<string> ChargeAsync(string reference, decimal amount, CancellationToken cancellationToken = default);
    }
}
