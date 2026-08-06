using ACST.Domain.DTOs.Auth;
using ACST.Shared;

namespace ACST.Domain.Features.Auth
{
    public interface IAuthService
    {
        Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request);
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
        Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Result> UpdateProfileAsync(int userId, UpdateProfileRequest request);
        Task<Result<List<UserAccountResponse>>> GetAllUsersAsync();
    }
}
