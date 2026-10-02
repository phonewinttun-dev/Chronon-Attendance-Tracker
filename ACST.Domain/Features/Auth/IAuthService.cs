using ACST.Domain.DTOs.Auth;
using ACST.Shared;

namespace ACST.Domain.Features.Auth
{
    public interface IAuthService
    {
        Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
        Task<Result<List<UserAccountResponse>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<Result<UserAccountResponse>> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
    }
}

