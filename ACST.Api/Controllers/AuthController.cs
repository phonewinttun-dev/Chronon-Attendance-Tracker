using ACST.Domain.DTOs.Auth;
using ACST.Domain.Features.Auth;
using ACST.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers
{
    /// <summary>
    /// Authentication and user account management controller.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="request">User registration credentials.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JWT authentication result upon successful creation.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Authenticates user credentials and issues access/refresh tokens.
        /// </summary>
        /// <param name="request">User login credentials.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JWT access and refresh tokens.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Rotates an expired access token using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New JWT access and refresh tokens.</returns>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _authService.RefreshTokenAsync(request, cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all registered system user accounts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of system user accounts.</returns>
        [HttpGet("users")]
        [ProducesResponseType(typeof(Result<List<UserAccountResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<UserAccountResponse>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
        {
            var result = await _authService.GetAllUsersAsync(cancellationToken);
            if (result.IsFailure) return BadRequest(result);
            return Ok(result);
        }
    }
}

