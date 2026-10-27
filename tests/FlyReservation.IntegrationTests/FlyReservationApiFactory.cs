using FlyReservation.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FlyReservation.IntegrationTests
{
    // Starts the real API in memory, backed by a throwaway PostgreSQL container
    public class FlyReservationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Point the API at the test container instead of the development database
            builder.UseSetting("ConnectionStrings:FlyReservationDb", _postgres.GetConnectionString());
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }
    }
}
