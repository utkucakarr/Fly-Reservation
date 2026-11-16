using FlyReservation.Api.Auth;
using FlyReservation.Api.Data;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace FlyReservation.IntegrationTests
{
    // Starts the real API in memory, backed by a throwaway PostgreSQL container
    public class FlyReservationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
        private readonly RedisContainer _redis = new RedisBuilder("redis:7.4").Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Point the API at the test container instead of the development database
            builder.UseSetting("ConnectionStrings:FlyReservationDb", _postgres.GetConnectionString());

            // A throwaway Redis too, so tests never share cache entries with the development Redis
            builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

            // The real key is a user secret and does not exist on the CI machine. Any 32+ byte text works for tests
            builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789");
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();

            // The admin the tests log in as (ApiTestHelpers.GetAdminTokenAsync)
            await AdminSeeder.SeedAsync(Services, ApiTestHelpers.AdminEmail, ApiTestHelpers.AdminPassword);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await _redis.DisposeAsync();
        }

        // Moves a reservation's deadline into the past, so tests don't have to wait 15 minutes
        public async Task MakeOverdueAsync(int reservationId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Reservations
                .Where(r => r.Id == reservationId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.PaymentDueAt, DateTime.UtcNow.AddMinutes(-1)));
        }

        // One run of the expiry job, in its own scope like the worker does it
        public async Task RunExpiryAsync()
        {
            using var scope = Services.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

            await reservationService.ExpireOverdueAsync();
        }
    }
}
