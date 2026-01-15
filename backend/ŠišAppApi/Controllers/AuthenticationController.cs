using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models.Authentication;
using ŠišAppApi.Services;

namespace ŠišAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
        {
            var response = await _authService.Login(request);
            if (response == null)
            {
                return Unauthorized();
            }

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

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] string refreshToken)
        {
            var result = await _authService.RevokeToken(refreshToken);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
    }
} 