using FlyReservation.Api.Caching;
using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FlyReservation.Api.Services
{
    public class FlightService : IFlightService
    {
        private readonly AppDbContext _context;
        private readonly HybridCache _cache;

        public FlightService(AppDbContext context, HybridCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<PagedResponse<FlightResponse>> GetAllAsync(FlightListRequest request)
        {
            var totalCount = await _context.Flights.CountAsync();

            var flights = await _context.Flights
                .AsNoTracking()
                .OrderBy(f => f.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();


            return new PagedResponse<FlightResponse>(flights.Select(ToResponse).ToList(), request.Page, request.PageSize, totalCount);
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
            await _cache.RemoveByTagAsync(CacheTags.FlightSearch);

            return ToResponse(flight);
        }

        public async Task UpdateAsync(int id, UpdateFlightRequest request)
        {
            var flight = await FindAsync(id);

            flight.ChangeTotalSeats(request.TotalSeats);
            flight.FlightNumber = request.FlightNumber;
            flight.Origin = request.Origin;
            flight.Destination = request.Destination;
            flight.DepartureTime = request.DepartureTime;
            flight.ArrivalTime = request.ArrivalTime;
            flight.Price = request.Price;

            await _context.SaveChangesAsync();
            await _cache.RemoveByTagAsync(CacheTags.FlightSearch);
        }

        public async Task DeleteAsync(int id)
        {
            var flight = await FindAsync(id);

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
            await _cache.RemoveByTagAsync(CacheTags.FlightSearch);
        }

        public async Task<List<FlightResponse>> SearchAsync(FlightSearchRequest request)
        {
            // Same route and day → same key. Only a cache miss runs the query; concurrent misses share one query
            var key = $"flights:search:{request.Origin}:{request.Destination}:{request.Date:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(
                key,
                async cancel => await SearchInDatabaseAsync(request, cancel),
                tags: [CacheTags.FlightSearch]);
        }

        private async Task<List<FlightResponse>> SearchInDatabaseAsync(FlightSearchRequest request, CancellationToken cancellationToken)
        {
            // A range on the raw column (not DepartureTime.Date == date), so an index on DepartureTime can be used
            var from = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = from.AddDays(1);

            var flights = await _context.Flights
                .AsNoTracking()
                .Where(f => f.Origin == request.Origin
                         && f.Destination == request.Destination
                         && f.DepartureTime >= from
                         && f.DepartureTime < to)
                .OrderBy(f => f.DepartureTime)
                .ToListAsync(cancellationToken);

            return flights.Select(ToResponse).ToList();
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
