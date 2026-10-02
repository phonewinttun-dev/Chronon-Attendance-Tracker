using ACST.Domain.DTOs.Auth;
using ACST.Domain.Features.Auth;
using ACST.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ACST.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth-policy")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _authService.RefreshTokenAsync(request, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
        {
            var result = await _authService.GetAllUsersAsync(cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId, CancellationToken cancellationToken = default)
        {
            var result = await _authService.DeleteUserAsync(userId, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }
    }
}

