using FlyReservation.Api.Exceptions;

namespace FlyReservation.Api.Entities
{
    public class Flight
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public List<Reservation> Reservations { get; set; } = new();

        // Concurrency token: mapped to PostgreSQL's built-in xmin column, which changes on every update of the row
        public uint Version { get; set; }

        // Seat rules live on the entity so they can be unit tested without a database

        public void ReserveSeat()
        {
            if (AvailableSeats <= 0)
                throw new ConflictException($"Flight {FlightNumber} has no available seats.");

            AvailableSeats--;
        }

        public void ReleaseSeat()
        {
            AvailableSeats++;
        }

        public void ChangeTotalSeats(int totalSeats)
        {
            var soldSeats = TotalSeats - AvailableSeats;
            if (totalSeats < soldSeats)
                throw new ConflictException($"Total seats cannot be less than the {soldSeats} seats already sold.");

            TotalSeats = totalSeats;
            AvailableSeats = totalSeats - soldSeats;
        }
    }
}
