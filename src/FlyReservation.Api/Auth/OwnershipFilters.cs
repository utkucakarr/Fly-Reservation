using FlyReservation.Api.Entities;

namespace FlyReservation.Api.Auth
{
    // "Is this record yours?" as part of the query's WHERE. An admin sees every row, a customer only their own.
    // Someone else's record is simply not found (404): a 403 would tell the caller that the id exists
    public static class OwnershipFilters
    {
        public static IQueryable<Passenger> VisibleTo(this IQueryable<Passenger> passengers, CurrentUser user)
        {
            return user.IsAdmin ? passengers : passengers.Where(p => p.UserId == user.Id);
        }

        // A reservation belongs to whoever owns its passenger
        public static IQueryable<Reservation> VisibleTo(this IQueryable<Reservation> reservations, CurrentUser user)
        {
            return user.IsAdmin ? reservations : reservations.Where(r => r.Passenger.UserId == user.Id);
        }
    }
}
