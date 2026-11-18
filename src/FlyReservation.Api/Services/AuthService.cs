using System.Security.Cryptography;
using System.Text;
using FlyReservation.Api.Auth;
using FlyReservation.Api.Data;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Entities;
using FlyReservation.Api.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FlyReservation.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly JwtOptions _options;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IPasswordHasher<User> passwordHasher, JwtTokenGenerator tokenGenerator,
            IOptions<JwtOptions> options, ILogger<AuthService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AuthTokens> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

            // Hashes the given password with the salt stored inside PasswordHash and compares the results
            if (user is null ||
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                throw new InvalidCredentialsException();
            }

            // A new login starts a new family
            return await IssueTokensAsync(user, familyId: Guid.NewGuid());
        }

        public async Task<AuthTokens> RefreshAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new InvalidRefreshTokenException();

            var tokenHash = Hash(refreshToken);
            var stored = await _context.RefreshTokens
                .AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (stored is null || stored.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidRefreshTokenException();

            // Check and act in one statement, like the seat counter: "mark it used, but only if nobody has yet".
            // Reading RevokedAt first and saving later lets parallel requests with the same token all pass
            // (measured: 16 of 20 got new tokens)
            var consumed = await _context.RefreshTokens
                .Where(t => t.Id == stored.Id && t.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, DateTime.UtcNow));

            if (consumed == 0)
            {
                // The token was already used, or revoked. A used token coming back means two parties hold it:
                // one of them stole it, and the server cannot tell which. End every session of that login
                await RevokeFamilyAsync(stored.FamilyId);
                _logger.LogWarning("Refresh token reuse for user {UserId}: token family {FamilyId} revoked", stored.UserId, stored.FamilyId);

                throw new InvalidRefreshTokenException();
            }

            return await IssueTokensAsync(stored.User, stored.FamilyId);
        }

        public async Task LogoutAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return;

            var tokenHash = Hash(refreshToken);
            var familyId = await _context.RefreshTokens
                .Where(t => t.TokenHash == tokenHash)
                .Select(t => (Guid?)t.FamilyId)
                .FirstOrDefaultAsync();

            if (familyId is not null)
                await RevokeFamilyAsync(familyId.Value);
        }

        private async Task<AuthTokens> IssueTokensAsync(User user, Guid familyId)
        {
            var (accessToken, accessTokenExpiresAt) = _tokenGenerator.Create(user);

            // 256 random bits. Not a JWT: it carries no data, it is only a key to a row in RefreshTokens
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var refreshTokenExpiresAt = DateTime.UtcNow.Add(_options.RefreshTokenLifetime);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = Hash(refreshToken),
                FamilyId = familyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshTokenExpiresAt
            });
            await _context.SaveChangesAsync();

            return new AuthTokens(accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt);
        }

        public async Task<AuthTokens> RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                throw new ConflictException("An account with this email already exists.");

            // The role is set here, never taken from the request
            var user = new User { Email = email, Role = Roles.Customer };
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Logged in right away: a new token family, like a login
            return await IssueTokensAsync(user, familyId: Guid.NewGuid());
        }

        private Task<int> RevokeFamilyAsync(Guid familyId)
        {
            return _context.RefreshTokens
                .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
        }

        // A fast hash is enough here: the token is 256 random bits, there is nothing to guess.
        // Slow hashes (PBKDF2) are for passwords, which people choose and reuse
        private static string Hash(string refreshToken)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        }
    }
}
