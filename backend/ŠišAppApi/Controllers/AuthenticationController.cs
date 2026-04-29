using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ŠišAppApi.Models.Authentication;
using ŠišAppApi.Models.DTOs.Auth;
using ŠišAppApi.Services;

namespace ŠišAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
[Authorize]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
        {
            var response = await _authService.Login(request);
            if (response == null)
            {
                return Unauthorized();
            }

            return Ok(response);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponse>> Register(RegisterDto request)
        {
            var response = await _authService.Register(request);
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] string refreshToken)
        {
            var response = await _authService.RefreshToken(refreshToken);
            if (response == null)
            {
                return Unauthorized();
            }

            return Ok(response);
        }

        [HttpPost("password-reset/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset(PasswordResetRequestDto request)
        {
            await _authService.RequestPasswordReset(request);
            return Ok(new { message = "Ako email postoji, poslan je token za reset lozinke." });
        }

        [HttpPost("password-reset/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmPasswordReset(PasswordResetConfirmDto request)
        {
            await _authService.ResetPassword(request);
            return Ok(new { message = "Lozinka je uspješno promijenjena." });
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] string? refreshToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var accessTokenJti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var result = await _authService.RevokeToken(refreshToken, userId, accessTokenJti);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
    }
} 