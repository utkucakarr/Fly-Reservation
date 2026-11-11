using FluentValidation;
using FlyReservation.Api.BackgroundServices;
using FlyReservation.Api.Data;
using FlyReservation.Api.ExceptionHandling;
using FlyReservation.Api.Filters;
using FlyReservation.Api.Payments;
using FlyReservation.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Replace the default logger with Serilog. Sinks and levels come from the "Serilog" section in appsettings
builder.Services.AddSerilog((_, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration));

// Add services to the container.

// FluentValidationFilter validates every request DTO before the controller action runs
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FlyReservationDb"))
        // EF Core logs every failed command as Error, even the ones GlobalExceptionHandler turns into 409.
        // Lower them to Debug; the handler logs real failures (500) as Error itself
        .ConfigureWarnings(warnings => warnings.Log(
            (RelationalEventId.CommandError, LogLevel.Debug),
            (CoreEventId.SaveChangesFailed, LogLevel.Debug))));

// L2 cache: Redis, shared by every API instance and kept across restarts.
// HybridCache picks up IDistributedCache automatically. Without a Redis connection string it runs with L1 (memory) only
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "flyreservation:";
    });
}

// L1 in-memory cache with stampede protection, in front of the L2 above (if any)
builder.Services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
{
    Expiration = TimeSpan.FromMinutes(5),             // L2 (Redis)
    LocalCacheExpiration = TimeSpan.FromMinutes(1)    // L1: short, another API instance's memory can be stale until it expires
});

// Business logic lives in services. Scoped = one instance per HTTP request (same as AppDbContext)
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
// How long an unpaid reservation holds a seat, and how often the worker looks for overdue ones
builder.Services.Configure<ReservationOptions>(builder.Configuration.GetSection("Reservations"));
builder.Services.AddHostedService<ReservationExpiryWorker>();
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["PaymentService:BaseUrl"]!))
    // Timeout, retry and circuit breaker around every call to the provider
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);           // one attempt
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);     // all attempts and waits together
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);               // grows exponentially, with jitter
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10); // must be at least 2x AttemptTimeout
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.FailureRatio = 0.9;                          // open only when the provider is (almost) down
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    });


// Catch unhandled exceptions in one place and return ProblemDetails responses
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// One summary line per request: method, path, status code, duration.
// Outside UseExceptionHandler so it logs the status code the client actually receives
app.UseSerilogRequestLogging();

// Wraps every middleware and controller below
app.UseExceptionHandler();

// Turn empty error responses (e.g. 404 for unknown routes) into ProblemDetails
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar: a page that reads that document and sends requests from the browser
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
