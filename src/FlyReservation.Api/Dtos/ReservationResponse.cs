namespace FlyReservation.Api.Dtos
{
    public record ReservationResponse(
    int Id,
    string PnrCode,
    string Status,
    decimal Price,
    DateTime? PaymentDueAt,
    DateTime CreatedAt,
    int FlightId,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    int PassengerId,
    string PassengerFullName);
}
