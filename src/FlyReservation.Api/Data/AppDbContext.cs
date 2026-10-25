using FlyReservation.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Flight> Flights => Set<Flight>();
    }
}
