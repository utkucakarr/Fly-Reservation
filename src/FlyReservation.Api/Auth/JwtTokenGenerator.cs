using System.Text;
using FlyReservation.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FlyReservation.Api.Auth
{
    public class JwtTokenGenerator
    {
        private readonly JwtOptions _options;

        public JwtTokenGenerator(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public (string Token, DateTime ExpiresAt) Create(User user)
        {
            var expiresAt = DateTime.UtcNow.Add(_options.AccessTokenLifetime);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,       // who made the token
                Audience = _options.Audience,   // who it is for
                Expires = expiresAt,
                // The payload is only Base64, anyone can read it: never put a password or a passport number here
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                    [JwtRegisteredClaimNames.Email] = user.Email,
                    ["role"] = user.Role
                },
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
        }
    }
}
