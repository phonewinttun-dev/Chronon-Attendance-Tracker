using ACST.Database.ApplicationDbContextModels.Models;
using System.Security.Claims;

namespace ACST.Domain.Features.Auth
{
    public interface ITokenService
    {
        string GenerateAccessToken(TblUser user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
