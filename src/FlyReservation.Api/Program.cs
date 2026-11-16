using System.Text;
using FluentValidation;
using FlyReservation.Api.Auth;
using FlyReservation.Api.BackgroundServices;
using FlyReservation.Api.Data;
using FlyReservation.Api.Entities;
using FlyReservation.Api.ExceptionHandling;
using FlyReservation.Api.Filters;
using FlyReservation.Api.Payments;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
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

// Authentication: who is calling? Reads "Authorization: Bearer <token>" and checks the token's signature and lifetime.
// Nothing is looked up in the database: the signature proves that this API made the token and nobody changed it
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 bytes. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<long random text>\"");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names as they are in the token ("sub", "role") instead of the long XML-era names
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),   // default is 5 minutes: an expired token would keep working that long
            NameClaimType = "sub",
            RoleClaimType = "role"                  // [Authorize(Roles = ...)] reads this claim
        };
    });
// Authorization: is this caller allowed to do this? [Authorize(Roles = ...)] on the admin actions
builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
// PBKDF2-HMAC-SHA512. 210,000 is OWASP's current number for it; the default is 100,000
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);

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

// Tells the browser not to guess the content type: a JSON response must never be run as HTML or script
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next();
});

// Order matters: first find out who the caller is (401 if a token is needed and missing or invalid),
// then check what they may do (403 if the token is fine but the role is not)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// The first admin, from configuration (user secrets). Skipped when not configured or when the user already exists
var adminEmail = app.Configuration["AdminUser:Email"];
var adminPassword = app.Configuration["AdminUser:Password"];
if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    await AdminSeeder.SeedAsync(app.Services, adminEmail, adminPassword);

app.Run();
