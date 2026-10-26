using FlyReservation.Api.Data;
using FlyReservation.Api.ExceptionHandling;
using FlyReservation.Api.Filters;
using FlyReservation.Api.Services;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// FluentValidationFilter validates every request DTO before the controller action runs
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationFilter>());

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FlyReservationDb")));

// Business logic lives in services. Scoped = one instance per HTTP request (same as AppDbContext)
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

// Catch unhandled exceptions in one place and return ProblemDetails responses
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Must be first so it wraps every middleware and controller below
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
