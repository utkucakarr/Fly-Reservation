namespace FlyReservation.Api.Entities
{
    public class Passenger
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;

        // Concurrency token for admin edits (ETag / If-Match). Only PassengerService.UpdateAsync increases it.
        // xmin would work today (bookings never write this row), but one pattern for flights and passengers
        // keeps working if something else starts writing passenger rows later
        public int Version { get; set; }

        public List<Reservation> Reservations { get; set; } = new();
    }
}
