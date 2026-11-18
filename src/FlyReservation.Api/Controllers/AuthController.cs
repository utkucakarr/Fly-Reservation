using FlyReservation.Api.Auth;
using FlyReservation.Api.Dtos;
using FlyReservation.Api.Filters;
using FlyReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlyReservation.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // A new customer account, logged in right away: the same answer as login.
        // 200, not 201: there is no address to read the new account from
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]       // the email already has an account
        public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
        {
            return Respond(await _authService.RegisterAsync(request));
        }

        // Email + password in. Out: a short-lived access token in the body (sent back as "Authorization: Bearer <token>")
        // and a long-lived refresh token in an HttpOnly cookie
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // unknown email or wrong password
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            return Respond(await _authService.LoginAsync(request));
        }

        // A new access token without the password. The refresh token is replaced too (rotation): each one works once
        [HttpPost("refresh")]
        [RequireCsrfHeader]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]     // no X-Requested-With header
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]   // no cookie, or an unknown, expired or already used token
        public async Task<ActionResult<LoginResponse>> Refresh()
        {
            return Respond(await _authService.RefreshAsync(Request.Cookies[RefreshTokenCookie.Name]));
        }

        // Ends the session on the server: the refresh token stops working. The access token still lives until it expires
        [HttpPost("logout")]
        [RequireCsrfHeader]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]     // no X-Requested-With header
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(Request.Cookies[RefreshTokenCookie.Name]);

            RefreshTokenCookie.Delete(Response);
            return NoContent();
        }

        private ActionResult<LoginResponse> Respond(AuthTokens tokens)
        {
            RefreshTokenCookie.Append(Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

            // Tokens must never be stored by a proxy or the browser cache
            Response.Headers.CacheControl = "no-store";
            return Ok(new LoginResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
        }
    }
}
