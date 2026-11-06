namespace FlyReservation.Api.Entities
{
    public enum ReservationStatus
    {
        PendingPayment,   // holds a seat until PaymentDueAt
        Confirmed,        // the seat is final
        Expired           // not paid in time, the seat went back on sale
    }
}
