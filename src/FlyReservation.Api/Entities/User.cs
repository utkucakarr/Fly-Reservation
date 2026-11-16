namespace FlyReservation.Api.Entities
{
    // Someone who can log in. Today only admins: passengers book without an account
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;

        // Never the password itself: a salted, slow hash (PBKDF2) made by PasswordHasher
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
