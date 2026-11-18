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
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Flight>(entity =>
            {
                entity.Property(f => f.FlightNumber).HasMaxLength(10);
                entity.Property(f => f.Origin).HasMaxLength(3);
                entity.Property(f => f.Destination).HasMaxLength(3);
                entity.Property(f => f.Price).HasPrecision(10, 2);

                entity.HasIndex(f => new { f.FlightNumber, f.DepartureTime }).IsUnique();
                // Flight search: equality columns first, the range column (DepartureTime) last
                entity.HasIndex(f => new { f.Origin, f.Destination, f.DepartureTime });

                // Optimistic concurrency: EF Core adds "WHERE "Version" = <original value>" to every UPDATE and DELETE of a flight.
                // Not xmin: xmin changes on every write to the row, bookings included (false 412s on busy flights)
                entity.Property(f => f.Version).IsConcurrencyToken();

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

                // Optimistic concurrency, same as flights
                entity.Property(p => p.Version).IsConcurrencyToken();

                // Deleting an account must not silently delete its passengers and their bookings
                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Email).HasMaxLength(100);
                entity.Property(u => u.PasswordHash).HasMaxLength(200);
                entity.Property(u => u.Role).HasMaxLength(20);

                // Emails are stored lower-case, so this also stops "Admin@..." next to "admin@..."
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(t => t.TokenHash).HasMaxLength(64);     // SHA-256 as hex

                // Every refresh looks the token up by its hash
                entity.HasIndex(t => t.TokenHash).IsUnique();
                // Revoking a family (reuse, logout)
                entity.HasIndex(t => t.FamilyId);

                // Deleting a user ends all of their sessions
                entity.HasOne(t => t.User)
                      .WithMany()
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.Property(r => r.PnrCode).HasMaxLength(6);
                entity.Property(r => r.IdempotencyKey).HasMaxLength(100);
                entity.Property(r => r.PaymentTransactionId).HasMaxLength(50);
                // Stored as text: readable in psql, and adding a status never renumbers the old rows
                entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(r => r.Price).HasPrecision(10, 2);

                // The last line of defence: a reservation without a price must never reach the payment provider.
                // Defense in depth
                entity.ToTable(t => t.HasCheckConstraint("CK_Reservations_Price", "\"Price\" > 0"));

                entity.HasIndex(r => r.IdempotencyKey).IsUnique();
                entity.HasIndex(r => r.PnrCode).IsUnique();

                // A passenger can hold only one seat on a flight. Also stops a double click from booking twice.
                // Expired rows don't count: that passenger may book the flight again
                entity.HasIndex(r => new { r.FlightId, r.PassengerId })
                      .IsUnique()
                      .HasFilter("\"Status\" <> 'Expired'");

                // The expiry job only looks at pending rows, so only they are indexed
                entity.HasIndex(r => r.PaymentDueAt).HasFilter("\"Status\" = 'PendingPayment'");
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
