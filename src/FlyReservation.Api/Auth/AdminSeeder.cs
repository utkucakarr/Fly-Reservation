using FlyReservation.Api.Data;
using FlyReservation.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlyReservation.Api.Auth
{
    // There is no "register as admin" endpoint (anyone could call it), so the first admin comes from configuration
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, string email, string password)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            email = email.Trim().ToLowerInvariant();
            if (await context.Users.AnyAsync(u => u.Email == email))
                return;

            var admin = new User { Email = email, Role = Roles.Admin };
            admin.PasswordHash = passwordHasher.HashPassword(admin, password);

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
