using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Services
{
    public class FlightService : IFlightService
    {
        private readonly AppDbContext _context;

        public FlightService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FlightResponse>> GetAllAsync()
        {
            var flights = await _context.Flights.ToListAsync();
            return flights.Select(ToResponse).ToList();
        }

        public async Task<FlightResponse> GetByIdAsync(int id)
        {
            var flight = await FindAsync(id);
            return ToResponse(flight);
        }

        public async Task<FlightResponse> CreateAsync(CreateFlightRequest request)
        {
            var flight = new Flight
            {
                FlightNumber = request.FlightNumber,
                Origin = request.Origin,
                Destination = request.Destination,
                DepartureTime = request.DepartureTime,
                ArrivalTime = request.ArrivalTime,
                TotalSeats = request.TotalSeats,
                AvailableSeats = request.TotalSeats,
                Price = request.Price
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            return ToResponse(flight);
        }

        public async Task UpdateAsync(int id, UpdateFlightRequest request)
        {
            var flight = await FindAsync(id);

            var soldSeats = flight.TotalSeats - flight.AvailableSeats;
            if (request.TotalSeats < soldSeats)
                throw new ConflictException($"Total seats cannot be less than the {soldSeats} seats already sold.");

            flight.FlightNumber = request.FlightNumber;
            flight.Origin = request.Origin;
            flight.Destination = request.Destination;
            flight.DepartureTime = request.DepartureTime;
            flight.ArrivalTime = request.ArrivalTime;
            flight.TotalSeats = request.TotalSeats;
            flight.AvailableSeats = request.TotalSeats - soldSeats;
            flight.Price = request.Price;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var flight = await FindAsync(id);

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
        }

        private async Task<Flight> FindAsync(int id)
        {
            return await _context.Flights.FindAsync(id)
                ?? throw new NotFoundException($"Flight {id} not found.");
        }

        private static FlightResponse ToResponse(Flight flight)
        {
            return new FlightResponse(
                flight.Id,
                flight.FlightNumber,
                flight.Origin,
                flight.Destination,
                flight.DepartureTime,
                flight.ArrivalTime,
                flight.TotalSeats,
                flight.AvailableSeats,
                flight.Price);
        }
    }
}
