namespace FlyReservation.Api.Dtos
{
    public record FlightResponse(
        int Id,
        string FlightNumber,
        string Origin,
        string Destination,
        DateTime DepartureTime,
        DateTime ArrivalTime,
        int TotalSeats,
        int AvailableSeats,
        decimal Price);
}
