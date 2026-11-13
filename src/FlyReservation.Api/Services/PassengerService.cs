using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Services
{
    public class PassengerService : IPassengerService
    {
        private readonly AppDbContext _context;

        public PassengerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PassengerResponse>> GetAllAsync()
        {
            var passengers = await _context.Passengers.AsNoTracking().ToListAsync();
            return passengers.Select(ToResponse).ToList();
        }

        public async Task<(PassengerResponse Passenger, int Version)> GetByIdAsync(int id)
        {
            var passenger = await FindAsync(id);
            return (ToResponse(passenger), passenger.Version);
        }

        public async Task<PassengerResponse> CreateAsync(CreatePassengerRequest request)
        {
            var passenger = new Passenger
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PassportNumber = request.PassportNumber
            };

            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            return ToResponse(passenger);
        }

        public async Task<int> UpdateAsync(int id, UpdatePassengerRequest request, int expectedVersion)
        {
            var passenger = await FindAsync(id);

            // Same as FlightService.UpdateAsync: the WHERE checks the version the client read (If-Match)
            _context.Entry(passenger).Property(p => p.Version).OriginalValue = expectedVersion;
            passenger.Version = expectedVersion + 1;

            passenger.FirstName = request.FirstName;
            passenger.LastName = request.LastName;
            passenger.Email = request.Email;
            passenger.PassportNumber = request.PassportNumber;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // 0 rows updated: someone else saved the passenger after the client read it
                throw new PreconditionFailedException(
                    $"Passenger {id} was changed after you read it. Get it again and reapply your changes.");
            }

            return passenger.Version;
        }

        public async Task DeleteAsync(int id)
        {
            var passenger = await FindAsync(id);

            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();
        }

        private async Task<Passenger> FindAsync(int id)
        {
            return await _context.Passengers.FindAsync(id)
                ?? throw new NotFoundException($"Passenger {id} not found.");
        }

        private static PassengerResponse ToResponse(Passenger passenger)
        {
            return new PassengerResponse(passenger.Id, passenger.FirstName, passenger.LastName, passenger.Email);
        }
    }
}
