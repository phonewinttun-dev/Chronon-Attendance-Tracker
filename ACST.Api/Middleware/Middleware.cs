using ACST.Domain.DTOs.Auth;
using ACST.Domain.Features.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ACST.Api.Middleware
{
    public class Middleware
    {
        private static readonly JwtSecurityTokenHandler TokenHandler = new();
        private readonly RequestDelegate _next;
        private readonly TokenValidationParameters _validationParameters;

        public Middleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;

            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "default_secret_key_at_least_32_chars_long";
            var key = Encoding.UTF8.GetBytes(secretKey);

            _validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path != null && (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring(7).Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    ValidateToken(token, context);
                }
            }

            await _next(context);
        }

        private bool ValidateToken(string token, HttpContext context)
        {
            try
            {
                var principal = TokenHandler.ValidateToken(token, _validationParameters, out SecurityToken validatedToken);
                context.User = principal;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
