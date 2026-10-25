namespace FlyReservation.Api.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public string PnrCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public int FlightId { get; set; }
        public Flight Flight { get; set; } = null!;

        public int PassengerId { get; set; }
        public Passenger Passenger { get; set; } = null!;
    }
}
