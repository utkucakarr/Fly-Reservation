using FluentValidation;
using FlyReservation.Api.Data;
using FlyReservation.Api.ExceptionHandling;
using FlyReservation.Api.Filters;
using FlyReservation.Api.Payments;
using FlyReservation.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
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
// Typed HttpClient for the external payment provider (created and pooled by IHttpClientFactory)
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["PaymentService:BaseUrl"]!));

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
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
