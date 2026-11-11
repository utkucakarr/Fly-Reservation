using BenchmarkDotNet.Attributes;
using FlyReservation.Api.Data;
using FlyReservation.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlyReservation.Benchmarks
{
    // The flight search on 100,000 flights, measured from C#: EF Core + Npgsql + network + PostgreSQL + objects.
    // EXPLAIN ANALYZE shows only the PostgreSQL part of this.
    // Each case gets its own throwaway PostgreSQL container, so the development database is never touched
    [MemoryDiagnoser]
    public class FlightSearchBenchmarks
    {
        private static readonly DateTime From = new(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime To = From.AddDays(1);

        private PostgreSqlContainer _postgres = null!;
        private DbContextOptions<AppDbContext> _options = null!;

        [Params(false, true)]
        public bool WithIndex { get; set; }

        [GlobalSetup]
        public async Task SetupAsync()
        {
            _postgres = new PostgreSqlBuilder("postgres:17").Build();
            await _postgres.StartAsync();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            await using var context = new AppDbContext(_options);
            await context.Database.MigrateAsync();

            // setseed: random() returns the same numbers every run, so every run searches the same flights.
            // It must run in the same command as the INSERT (same connection). Our own script, no user input
            var seedSql = "SELECT setseed(0.42);\n" + ReadSeedScript();
            await context.Database.ExecuteSqlRawAsync(seedSql);

            if (!WithIndex)
            {
                // Both: without the second one, the planner walks that index for the date range instead of a Seq Scan
                await context.Database.ExecuteSqlRawAsync("""
                    DROP INDEX "IX_Flights_Origin_Destination_DepartureTime";
                    DROP INDEX "IX_Flights_FlightNumber_DepartureTime";
                    """);
            }

            // Fresh statistics, and autovacuum has nothing left to do in the middle of the measurement
            await context.Database.ExecuteSqlRawAsync("""VACUUM ANALYZE "Flights";""");

            await CheckWhatIsMeasuredAsync();
        }

        [GlobalCleanup]
        public async Task CleanupAsync() => await _postgres.DisposeAsync();

        // The same query as FlightService.SearchInDatabaseAsync. A new DbContext per call, like one per HTTP request
        [Benchmark]
        public async Task<List<Flight>> Search()
        {
            await using var context = new AppDbContext(_options);

            return await context.Flights
                .AsNoTracking()
                .Where(f => f.Origin == "IST"
                         && f.Destination == "AYT"
                         && f.DepartureTime >= From
                         && f.DepartureTime < To)
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();
        }

        // A benchmark can measure the wrong thing without any error. Fail if the search finds nothing,
        // and write the plan to the log: Seq Scan without the index, Bitmap Index Scan with it
        private async Task CheckWhatIsMeasuredAsync()
        {
            var flights = await Search();
            if (flights.Count == 0)
                throw new InvalidOperationException("The search found no flights, the benchmark would measure an empty result.");

            await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
            await connection.OpenAsync();

            // The same search as the LINQ query in Search()
            await using var command = new NpgsqlCommand("""
                EXPLAIN SELECT * FROM "Flights"
                WHERE "Origin" = 'IST' AND "Destination" = 'AYT'
                  AND "DepartureTime" >= '2026-12-15 00:00+00' AND "DepartureTime" < '2026-12-16 00:00+00'
                ORDER BY "DepartureTime"
                """, connection);
            await using var reader = await command.ExecuteReaderAsync();

            Console.WriteLine($"// WithIndex={WithIndex}: flights found: {flights.Count}. Plan:");
            while (await reader.ReadAsync())
                Console.WriteLine("//   " + reader.GetString(0));
        }

        private static string ReadSeedScript()
        {
            using var stream = typeof(FlightSearchBenchmarks).Assembly.GetManifestResourceStream("seed-flights.sql")
                ?? throw new InvalidOperationException("seed-flights.sql is not embedded.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
