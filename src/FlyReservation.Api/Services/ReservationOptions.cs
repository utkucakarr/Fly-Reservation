namespace FlyReservation.Api.Services
{
    // Bound from the "Reservations" section in appsettings
    public class ReservationOptions
    {
        public TimeSpan PaymentWindow { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan ExpiryCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    }
}
