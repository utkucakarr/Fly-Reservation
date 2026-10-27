using FlyReservation.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Data
{
    public static class FlightQueryExtensions
    {
        // Pessimistic lock: SELECT ... FOR UPDATE keeps the row locked until the surrounding transaction ends,
        // so concurrent requests for the same flight wait here and see the updated seat count afterwards.
        // Only works inside a transaction: without one the lock is released as soon as the query finishes.
        public static async Task<Flight?> FindForUpdateAsync(this DbSet<Flight> flights, int id)
        {
            return await flights
                .FromSql($"SELECT * FROM \"Flights\" WHERE \"Id\" = {id} FOR UPDATE")
                .FirstOrDefaultAsync();
        }
    }
}
