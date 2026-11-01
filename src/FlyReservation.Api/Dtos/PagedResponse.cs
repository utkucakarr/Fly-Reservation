namespace FlyReservation.Api.Dtos
{
    public record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount);
}
