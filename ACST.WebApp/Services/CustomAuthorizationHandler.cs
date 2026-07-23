using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ACST.WebApp.Models;

namespace ACST.WebApp.Services
{
    public class CustomAuthorizationHandler : DelegatingHandler
    {
        private readonly IJSRuntime _js;

        public CustomAuthorizationHandler(IJSRuntime js)
        {
            _js = js;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", "chronon_auth_session");
                if (!string.IsNullOrEmpty(json))
                {
                    var session = JsonSerializer.Deserialize<LoginResponseDto>(json);
                    if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CustomAuthorizationHandler] Warning: Could not retrieve auth session token: {ex.Message}");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
