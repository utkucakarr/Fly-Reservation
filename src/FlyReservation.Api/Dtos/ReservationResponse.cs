namespace FlyReservation.Api.Dtos
{
    public record ReservationResponse(
    int Id,
    string PnrCode,
    DateTime CreatedAt,
    int FlightId,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    int PassengerId,
    string PassengerFullName);
}
