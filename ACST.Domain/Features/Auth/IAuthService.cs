using ACST.Domain.DTOs.Auth;
using ACST.Shared;

namespace ACST.Domain.Features.Auth
{
    /// <summary>
    /// Service contract for user authentication, registration, token rotation, and profile management.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user account and automatically logs them in.
        /// </summary>
        /// <param name="request">The registration request containing user details and credentials.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A result containing login credentials and JWT tokens if successful.</returns>
        Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticates user credentials and issues JWT access and refresh tokens.
        /// </summary>
        /// <param name="request">The login request with email and password.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A result containing user information and JWT tokens if successful.</returns>
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotates an expired access token using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token request payload.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A result containing new JWT access and refresh tokens.</returns>
        Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the profile information of an existing user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="request">The updated profile details.</param>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A result indicating operation success or failure.</returns>
        Task<Result> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all registered system user accounts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel operation.</param>
        /// <returns>A result containing the list of user account records.</returns>
        Task<Result<List<UserAccountResponse>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    }
}

