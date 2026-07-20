using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Auth;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ACST.Domain.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, ITokenService tokenService, IConfiguration configuration)
        {
            _context = context;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        public async Task<Result<UserAccountResponse>> RegisterAsync(RegisterRequest request)
        {
            if (await _context.TblUsers.AnyAsync(u => u.Email == request.Email && u.DeleteFlag != true))
            {
                return Result<UserAccountResponse>.Failure("Email is already registered.");
            }

            var role = await _context.TblRoles.FirstOrDefaultAsync(r => r.RoleId == request.RoleId && r.DeleteFlag != true);
            if (role == null)
            {
                return Result<UserAccountResponse>.Failure("Invalid Role selected.");
            }

            var passwordHash = HashPassword(request.Password);

            var user = new TblUser
            {
                Email = request.Email,
                FullName = request.FullName,
                MobileNum = request.MobileNum,
                PasswordHash = passwordHash,
                RoleId = request.RoleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeleteFlag = false
            };

            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserAccountResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                MobileNum = user.MobileNum,
                RoleId = user.RoleId,
                RoleName = role.RoleName,
                CreatedAt = user.CreatedAt
            };

            return Result<UserAccountResponse>.Success(response, "Account registered successfully.");
        }

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _context.TblUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.DeleteFlag != true);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash ?? ""))
            {
                return Result<LoginResponse>.Failure("Invalid email or password.");
            }

            // Retrieve Permissions for user role
            var permissions = await _context.TblRolepermissions
                .Where(rp => rp.RoleId == user.RoleId && rp.DeleteFlag != true && rp.Permission != null && rp.Permission.PermissionName != null)
                .Select(rp => rp.Permission!.PermissionName!)
                .ToListAsync();

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshTokenString = _tokenService.GenerateRefreshToken();

            // Store Refresh Token
            var userToken = new TblUsertoken
            {
                UserId = user.UserId,
                RefreshToken = refreshTokenString,
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"] ?? "7")),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeleteFlag = false
            };

            _context.TblUsertokens.Add(userToken);

            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                RoleName = user.Role?.RoleName ?? "",
                Permissions = permissions
            };

            return Result<LoginResponse>.Success(response, "Login successful.");
        }

        public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var userToken = await _context.TblUsertokens
                .Include(ut => ut.User)
                .ThenInclude(u => u.Role!)
                .FirstOrDefaultAsync(ut => ut.RefreshToken == request.RefreshToken && ut.DeleteFlag != true);

            if (userToken == null || userToken.IsRevoked == true || userToken.ExpiresAt < DateTime.UtcNow || userToken.User == null || userToken.User.DeleteFlag == true)
            {
                return Result<LoginResponse>.Failure("Invalid or expired refresh token.");
            }

            // Generate new access and refresh token (Token Rotation)
            var user = userToken.User;

            var permissions = await _context.TblRolepermissions
                .Where(rp => rp.RoleId == user.RoleId && rp.DeleteFlag != true && rp.Permission != null && rp.Permission.PermissionName != null)
                .Select(rp => rp.Permission!.PermissionName!)
                .ToListAsync();

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();

            // Revoke current refresh token
            userToken.IsRevoked = true;
            userToken.UpdatedAt = DateTime.UtcNow;

            // Add new refresh token
            var newUsertoken = new TblUsertoken
            {
                UserId = user.UserId,
                RefreshToken = newRefreshTokenString,
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"] ?? "7")),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeleteFlag = false
            };

            _context.TblUsertokens.Add(newUsertoken);
            await _context.SaveChangesAsync();

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                RoleName = user.Role?.RoleName ?? "",
                Permissions = permissions
            };

            return Result<LoginResponse>.Success(response, "Tokens refreshed successfully.");
        }

        public async Task<Result> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.UserId == userId && u.DeleteFlag != true);
            if (user == null)
            {
                return Result.Failure("User account not found.");
            }

            user.FullName = request.FullName;
            user.MobileNum = request.MobileNum;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result.Success("Profile updated successfully.");
        }

        public async Task<Result<List<UserAccountResponse>>> GetAllUsersAsync()
        {
            var users = await _context.TblUsers
                .Where(u => u.DeleteFlag != true)
                .Include(u => u.Role)
                .Select(u => new UserAccountResponse
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? "",
                    MobileNum = u.MobileNum,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : "",
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Result<List<UserAccountResponse>>.Success(users);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
