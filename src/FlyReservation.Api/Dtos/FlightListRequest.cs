namespace FlyReservation.Api.Dtos
{
    public record FlightListRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
