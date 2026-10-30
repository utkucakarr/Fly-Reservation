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
        public DbSet<Passenger> Passengers => Set<Passenger>();
        public DbSet<Reservation> Reservations => Set<Reservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Flight>(entity =>
            {
                entity.Property(f => f.FlightNumber).HasMaxLength(10);
                entity.Property(f => f.Origin).HasMaxLength(3);
                entity.Property(f => f.Destination).HasMaxLength(3);
                entity.Property(f => f.Price).HasPrecision(10, 2);

                entity.HasIndex(f => new { f.FlightNumber, f.DepartureTime }).IsUnique();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Flights_AvailableSeats",
                    "\"AvailableSeats\" >= 0 AND \"AvailableSeats\" <= \"TotalSeats\""));
            });

            modelBuilder.Entity<Passenger>(entity =>
            {
                entity.Property(p => p.FirstName).HasMaxLength(50);
                entity.Property(p => p.LastName).HasMaxLength(50);
                entity.Property(p => p.Email).HasMaxLength(100);
                entity.Property(p => p.PassportNumber).HasMaxLength(20);

                entity.HasIndex(p => p.Email).IsUnique();
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.Property(r => r.PnrCode).HasMaxLength(6);
                entity.Property(r => r.IdempotencyKey).HasMaxLength(100);
                entity.HasIndex(r => r.IdempotencyKey).IsUnique();
                entity.HasIndex(r => r.PnrCode).IsUnique();
                // A passenger can hold only one seat on a flight. Also stops a double click from booking twice
                entity.HasIndex(r => new { r.FlightId, r.PassengerId }).IsUnique();

                entity.HasOne(r => r.Flight)
                      .WithMany(f => f.Reservations)
                      .HasForeignKey(r => r.FlightId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Passenger)
                      .WithMany(p => p.Reservations)
                      .HasForeignKey(r => r.PassengerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
