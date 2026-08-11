using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ACST.WebApp.Models;

namespace ACST.WebApp.Services
{
    public class AuthStateService
    {
        private readonly IJSRuntime _js;
        private const string SessionKey = "chronon_auth_session";
        private const string RoleOverrideKey = "chronon_role_override";

        public event Action? OnChange;

        public LoginResponseDto? CurrentUser { get; private set; }
        public string ActiveRole { get; private set; } = "User";

        public bool IsLoggedIn => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.AccessToken);
        public bool IsAdmin => string.Equals(ActiveRole, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsActualAdmin => CurrentUser != null && string.Equals(CurrentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);

        public AuthStateService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var json = await GetStorageItemAsync(SessionKey);
                var roleOverride = await GetStorageItemAsync(RoleOverrideKey);

                if (!string.IsNullOrEmpty(json))
                {
                    CurrentUser = JsonSerializer.Deserialize<LoginResponseDto>(json);
                    if (CurrentUser != null)
                    {
                        if (IsAccessTokenExpired(CurrentUser.AccessToken))
                        {
                            CurrentUser = null;
                            ActiveRole = "User";
                            await RemoveStorageItemAsync(SessionKey);
                            await RemoveStorageItemAsync(RoleOverrideKey);
                        }
                        else
                        {
                            ActiveRole = !string.IsNullOrEmpty(roleOverride) ? roleOverride : CurrentUser.RoleName;
                        }
                    }
                }
                else
                {
                    CurrentUser = null;
                    if (!string.IsNullOrEmpty(roleOverride))
                    {
                        ActiveRole = roleOverride;
                    }
                }
            }
            catch { }

            NotifyStateChanged();
        }

        public async Task SetSessionAsync(LoginResponseDto session)
        {
            CurrentUser = session;
            ActiveRole = session.RoleName;

            try
            {
                var json = JsonSerializer.Serialize(session);
                await SetStorageItemAsync(SessionKey, json);
                await RemoveStorageItemAsync(RoleOverrideKey);
            }
            catch { }

            NotifyStateChanged();
        }

        public async Task SwitchRoleAsync(string newRole)
        {
            ActiveRole = newRole;
            try
            {
                await SetStorageItemAsync(RoleOverrideKey, newRole);
            }
            catch { }

            NotifyStateChanged();
        }

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            ActiveRole = "User";

            try
            {
                await RemoveStorageItemAsync(SessionKey);
                await RemoveStorageItemAsync(RoleOverrideKey);
            }
            catch { }

            NotifyStateChanged();
        }

        private bool IsAccessTokenExpired(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return true;
            }

            try
            {
                var parts = accessToken.Split('.');
                if (parts.Length != 3)
                {
                    return true;
                }

                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');
                var padding = (payload.Length % 4) switch
                {
                    2 => "==",
                    3 => "=",
                    _ => string.Empty
                };
                payload += padding;

                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("exp", out var expElement) || !expElement.TryGetInt64(out var expSeconds))
                {
                    return true;
                }

                var expiration = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                return expiration <= DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        private async Task<string?> GetStorageItemAsync(string key)
        {
            try
            {
                var val = await _js.InvokeAsync<string?>("localStorage.getItem", key);
                if (!string.IsNullOrEmpty(val)) return val;
            }
            catch { }

            try
            {
                return await _js.InvokeAsync<string?>("sessionStorage.getItem", key);
            }
            catch { }

            return null;
        }

        private async Task SetStorageItemAsync(string key, string value)
        {
            try { await _js.InvokeVoidAsync("localStorage.setItem", key, value); } catch { }
            try { await _js.InvokeVoidAsync("sessionStorage.setItem", key, value); } catch { }
        }

        private async Task RemoveStorageItemAsync(string key)
        {
            try { await _js.InvokeVoidAsync("localStorage.removeItem", key); } catch { }
            try { await _js.InvokeVoidAsync("sessionStorage.removeItem", key); } catch { }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}

