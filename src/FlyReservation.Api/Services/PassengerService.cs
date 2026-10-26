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
            var passengers = await _context.Passengers.ToListAsync();
            return passengers.Select(ToResponse).ToList();
        }

        public async Task<PassengerResponse> GetByIdAsync(int id)
        {
            var passenger = await FindAsync(id);
            return ToResponse(passenger);
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

        public async Task UpdateAsync(int id, UpdatePassengerRequest request)
        {
            var passenger = await FindAsync(id);

            passenger.FirstName = request.FirstName;
            passenger.LastName = request.LastName;
            passenger.Email = request.Email;
            passenger.PassportNumber = request.PassportNumber;

            await _context.SaveChangesAsync();
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
