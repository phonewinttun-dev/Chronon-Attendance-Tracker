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

        public AuthStateService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", SessionKey);
                var roleOverride = await _js.InvokeAsync<string?>("sessionStorage.getItem", RoleOverrideKey);

                if (!string.IsNullOrEmpty(json))
                {
                    CurrentUser = JsonSerializer.Deserialize<LoginResponseDto>(json);
                    if (CurrentUser != null)
                    {
                        ActiveRole = !string.IsNullOrEmpty(roleOverride) ? roleOverride : CurrentUser.RoleName;
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
                await _js.InvokeVoidAsync("sessionStorage.setItem", SessionKey, json);
                await _js.InvokeVoidAsync("sessionStorage.removeItem", RoleOverrideKey);
            }
            catch { }

            NotifyStateChanged();
        }

        public async Task SwitchRoleAsync(string newRole)
        {
            ActiveRole = newRole;
            try
            {
                await _js.InvokeVoidAsync("sessionStorage.setItem", RoleOverrideKey, newRole);
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
                await _js.InvokeVoidAsync("sessionStorage.removeItem", SessionKey);
                await _js.InvokeVoidAsync("sessionStorage.removeItem", RoleOverrideKey);
            }
            catch { }

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}

