using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Auth;
using ACST.Domain.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ACST.Domain.Tests.Features.Auth;

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
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "security")]
    public async Task RegisterAsync_ValidRequest_RegistersUserAndReturnsTokens()
    {
        // Arrange
        var role = new TblRole { RoleId = 1, RoleName = "User", DeleteFlag = false };
        _context.TblRoles.Add(role);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            FullName = "New User",
            Password = "Password123!",
            MobileNum = "+95911122233"
        };

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("New User", result.Data.FullName);
        Assert.Equal("newuser@example.com", result.Data.Email);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);

        var userInDb = await _context.TblUsers.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal("New User", userInDb.FullName);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "security")]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        _context.TblUsers.Add(new TblUser
        {
            Email = "existing@example.com",
            FullName = "Existing",
            DeleteFlag = false
        });
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            FullName = "Another User",
            Password = "Password123!"
        };

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email is already registered.", result.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "security")]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var role = new TblRole { RoleId = 1, RoleName = "Lecturer", DeleteFlag = false };
        _context.TblRoles.Add(role);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("SecretPass123");

        var user = new TblUser
        {
            UserId = 10,
            Email = "lecturer@example.com",
            FullName = "Lecturer Jane",
            PasswordHash = passwordHash,
            RoleId = 1,
            Role = role,
            DeleteFlag = false
        };
        _context.TblUsers.Add(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "lecturer@example.com",
            Password = "SecretPass123"
        };

        // Act
        var result = await _service.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Lecturer Jane", result.Data.FullName);
        Assert.Equal("mock.access.token", result.Data.AccessToken);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "security")]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

        _context.TblUsers.Add(new TblUser
        {
            Email = "user@example.com",
            PasswordHash = passwordHash,
            DeleteFlag = false
        });
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _service.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Message);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
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
    [Trait("Category", "negative")]
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
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
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
    [Trait("Category", "positive")]
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
    [Trait("Category", "negative")]
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
        public string GenerateAccessToken(TblUser user, IEnumerable<string>? permissions = null) => "mock.access.token";
        public string GenerateRefreshToken() => "mock.refresh.token";
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token) => new ClaimsPrincipal();
    }
}
