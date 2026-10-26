namespace FlyReservation.Api.Dtos
{
    public record CreateFlightRequest(
        string FlightNumber,
        string Origin,
        string Destination,
        DateTime DepartureTime,
        DateTime ArrivalTime,
        int TotalSeats,
        decimal Price);
}
