using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.Features.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ACST.Domain.Tests.Features.Auth;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;
    private const string ValidSecretKey = "SuperSecretKeyForTestingJwtTokens1234567890!";

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", ValidSecretKey },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:AccessTokenExpiryMinutes", "15" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public void GenerateAccessToken_ValidUserAndPermissions_GeneratesValidJwtWithClaims()
    {
        // Arrange
        var user = new TblUser
        {
            UserId = 42,
            FullName = "Jane Doe",
            Email = "jane.doe@example.com",
            Role = new TblRole { RoleId = 1, RoleName = "Lecturer" }
        };
        var permissions = new[] { "attendance.view", "attendance.mark" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, permissions);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, aud => aud == "TestAudience");
        Assert.Equal("42", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("jane.doe@example.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Jane Doe", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Lecturer", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);

        var permClaims = jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        Assert.Contains("attendance.view", permClaims);
        Assert.Contains("attendance.mark", permClaims);
    }

    [Fact]
    [Trait("Category", "negative")]
    public void GenerateAccessToken_MissingSecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();
        var serviceWithNoSecret = new TokenService(emptyConfig);
        var user = new TblUser { UserId = 1, FullName = "Test" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => serviceWithNoSecret.GenerateAccessToken(user));
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "security")]
    public void GenerateRefreshToken_GeneratesCryptographicallyStrong64ByteBase64String()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);

        var rawBytes = Convert.FromBase64String(token1);
        Assert.Equal(64, rawBytes.Length);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public void GetPrincipalFromExpiredToken_ValidToken_ExtractsPrincipalSuccessfully()
    {
        // Arrange
        var user = new TblUser
        {
            UserId = 99,
            FullName = "Expired User",
            Email = "expired@example.com",
            Role = new TblRole { RoleName = "Student" }
        };

        var token = _tokenService.GenerateAccessToken(user);

        // Act
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("99", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("expired@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "security")]
    public void GetPrincipalFromExpiredToken_InvalidSignature_ThrowsSecurityTokenException()
    {
        // Arrange: Token signed with a completely different key
        var otherSecretKey = "AnotherSecretKeyDifferentFromConfiguration1234!";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(otherSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            signingCredentials: credentials
        );
        var forgedToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        // Act & Assert
        Assert.ThrowsAny<SecurityTokenException>(() => _tokenService.GetPrincipalFromExpiredToken(forgedToken));
    }
}
