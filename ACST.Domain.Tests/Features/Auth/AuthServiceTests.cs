using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Auth;
using ACST.Domain.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ACST.Domain.Tests.Features.Auth
{
    public class AuthServiceTests
    {
        private readonly AppDbContext _context;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            var configuration = new ConfigurationBuilder().Build();
            var tokenService = new DummyTokenService();

            _service = new AuthService(_context, tokenService, configuration);
        }

        [Fact]
        public async Task DeleteUserAsync_UserExists_SoftDeletesUserAndReturnsSuccess()
        {
            // Arrange
            var role = new TblRole
            {
                RoleId = 1,
                RoleName = "Lecturer",
                DeleteFlag = false
            };
            _context.TblRoles.Add(role);

            var user = new TblUser
            {
                UserId = 1,
                FullName = "John Doe",
                Email = "john.doe@example.com",
                MobileNum = "+95912345678",
                RoleId = 1,
                Role = role,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                DeleteFlag = false
            };
            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteUserAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.UserId);
            Assert.Equal("John Doe", result.Data.FullName);
            Assert.Equal("john.doe@example.com", result.Data.Email);
            Assert.Equal("+95912345678", result.Data.MobileNum);
            Assert.Equal(1, result.Data.RoleId);
            Assert.Equal("Lecturer", result.Data.RoleName);
            Assert.Equal(user.CreatedAt, result.Data.CreatedAt);

            // Verify persistence in database
            var updatedUser = await _context.TblUsers.FirstOrDefaultAsync(u => u.UserId == 1);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.DeleteFlag);
            Assert.NotNull(updatedUser.UpdatedAt);
            Assert.True(updatedUser.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task DeleteUserAsync_UserDoesNotExist_ReturnsFailure()
        {
            // Act
            var result = await _service.DeleteUserAsync(999);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal("User account not found.", result.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_UserAlreadyDeleted_ReturnsFailure()
        {
            // Arrange
            var user = new TblUser
            {
                UserId = 2,
                FullName = "Already Deleted",
                Email = "deleted@example.com",
                DeleteFlag = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteUserAsync(2);

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal("User account not found.", result.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_UserWithoutRole_ReturnsSuccessWithNullRoleName()
        {
            // Arrange
            var user = new TblUser
            {
                UserId = 3,
                FullName = "User Without Role",
                Email = "norole@example.com",
                RoleId = null,
                Role = null,
                DeleteFlag = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteUserAsync(3);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.UserId);
            Assert.Null(result.Data.RoleId);
            Assert.Null(result.Data.RoleName);

            var updatedUser = await _context.TblUsers.FirstOrDefaultAsync(u => u.UserId == 3);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.DeleteFlag);
        }

        [Fact]
        public async Task DeleteUserAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
        {
            // Arrange
            var user = new TblUser
            {
                UserId = 4,
                FullName = "Cancel Test User",
                Email = "cancel@example.com",
                DeleteFlag = false
            };
            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await _service.DeleteUserAsync(4, cts.Token);
            });
        }

        private class DummyTokenService : ITokenService
        {
            public string GenerateAccessToken(TblUser user, IEnumerable<string>? permissions = null) => string.Empty;
            public string GenerateRefreshToken() => string.Empty;
            public ClaimsPrincipal GetPrincipalFromExpiredToken(string token) => new ClaimsPrincipal();
        }
    }
}
