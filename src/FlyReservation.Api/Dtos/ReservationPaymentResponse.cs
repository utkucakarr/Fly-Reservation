namespace FlyReservation.Api.Dtos
{
    public record ReservationPaymentResponse(string PnrCode, decimal Amount, string? TransactionId);
}
