namespace FlyReservation.Api.Dtos
{
    // Bound from the query string: GET /api/flights/search?origin=IST&destination=AYT&date=2026-12-15
    public record FlightSearchRequest
    {
        public string Origin { get; init; } = string.Empty;
        public string Destination { get; init; } = string.Empty;
        public DateOnly Date { get; init; }
    }
}
