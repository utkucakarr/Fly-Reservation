using FlyReservation.Api.Services;
using Microsoft.Extensions.Options;

namespace FlyReservation.Api.BackgroundServices
{
    // Runs for the lifetime of the API, with no HTTP request behind it: gives unpaid seats back
    public class ReservationExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval;
        private readonly ILogger<ReservationExpiryWorker> _logger;

        public ReservationExpiryWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<ReservationOptions> options,
            ILogger<ReservationExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _interval = options.Value.ExpiryCheckInterval;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // This class lives as long as the API, DbContext only as long as a scope. So one new scope per run
                    using var scope = _scopeFactory.CreateScope();
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                    await reservationService.ExpireOverdueAsync(stoppingToken);
                }
                // An exception that leaves ExecuteAsync stops the whole API. One failed run must not do that: the next tick tries again
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Reservation expiry run failed");
                }
            }
        }
    }
}
