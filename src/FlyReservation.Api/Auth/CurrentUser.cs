using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace FlyReservation.Api.Auth
{
    // Who is calling, read from the validated access token. Services get this instead of HttpContext
    public record CurrentUser(int Id, bool IsAdmin)
    {
        // Only for [Authorize] endpoints: there the token was validated and "sub" (the user's id) is always present
        public static CurrentUser From(ClaimsPrincipal principal)
        {
            var id = int.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
            return new CurrentUser(id, principal.IsInRole(Roles.Admin));
        }
    }
}