using ACST.Domain.DTOs.Auth;
using ACST.Domain.Features.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ACST.Api.Middleware
{
    public class Middleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public Middleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path != null && (path.StartsWith("/api/auth/login") || path.StartsWith("/api/auth/register") || path.StartsWith("/api/auth/refresh-token")))
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            var token = authHeader?.Split(" ").Last();
            bool isTokenValid = false;

            if (!string.IsNullOrEmpty(token))
            {
                isTokenValid = ValidateToken(token, context);
            }
            if (!isTokenValid)
            {
                var refreshToken = context.Request.Cookies["refreshToken"];
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    try
                    {
                        var authService = context.RequestServices.GetRequiredService<IAuthService>();
                        var result = await authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });

                        if (result.Data != null)
                        {
                            var tokenResponse = result.Data;

                            ValidateToken(tokenResponse.AccessToken, context);

                            context.Response.Headers.Append("X-Access-Token", tokenResponse.AccessToken);

                            var cookieOptions = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddDays(7)
                            };
                            context.Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, cookieOptions);
                        }
                    }
                    catch
                    {
                        // Refresh failed or service unavailable - continue unauthenticated
                    }
                }
            }

            await _next(context);
        }

        private bool ValidateToken(string token, HttpContext context)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"] ?? "default_secret_key_at_least_32_chars_long";
                var key = Encoding.UTF8.GetBytes(secretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

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
