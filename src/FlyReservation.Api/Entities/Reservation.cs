namespace FlyReservation.Api.Entities
{
    public class Reservation
    {
        // Sent by the client; a retry with the same key gets this reservation back instead of a new one
        public string? IdempotencyKey { get; set; }
        public int Id { get; set; }
        public string PnrCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int FlightId { get; set; }
        public Flight Flight { get; set; } = null!;

        public int PassengerId { get; set; }
        public Passenger Passenger { get; set; } = null!;
    }
}
