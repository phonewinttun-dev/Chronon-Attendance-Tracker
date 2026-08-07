using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
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
                string? json = null;
                try { json = await _js.InvokeAsync<string?>("localStorage.getItem", "chronon_auth_session"); } catch { }
                if (string.IsNullOrEmpty(json))
                {
                    try { json = await _js.InvokeAsync<string?>("sessionStorage.getItem", "chronon_auth_session"); } catch { }
                }

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

            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested || ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    throw;
                }

                bool isConnectionFailure = ex is HttpRequestException httpEx &&
                    (httpEx.InnerException is SocketException || httpEx.InnerException is IOException);

                if (!isConnectionFailure)
                {
                    throw;
                }

                // Attempt fallback port if primary port is unreachable (e.g. 7019 <-> 5211)
                var originalUri = request.RequestUri;
                if (originalUri != null)
                {
                    Uri? fallbackUri = null;
                    if (originalUri.Port == 7019)
                    {
                        fallbackUri = new UriBuilder(originalUri) { Scheme = "http", Port = 5211 }.Uri;
                    }
                    else if (originalUri.Port == 5211)
                    {
                        fallbackUri = new UriBuilder(originalUri) { Scheme = "https", Port = 7019 }.Uri;
                    }

                    if (fallbackUri != null)
                    {
                        HttpRequestMessage? fallbackRequest = null;
                        try
                        {
                            Console.WriteLine($"[CustomAuthorizationHandler] Connection failed to {originalUri}. Retrying with fallback: {fallbackUri}");
                            fallbackRequest = await CloneHttpRequestMessageAsync(request);
                            fallbackRequest.RequestUri = fallbackUri;

                            return await base.SendAsync(fallbackRequest, cancellationToken);
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"[CustomAuthorizationHandler] Fallback connection to {fallbackUri} also failed: {fallbackEx.Message}");
                            if (fallbackRequest != null && fallbackRequest != request)
                            {
                                fallbackRequest.Dispose();
                            }
                        }
                        finally
                        {
                            request.RequestUri = originalUri;
                        }
                    }
                }

                throw;
            }
        }

        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            if (request.Content != null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var prop in request.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);
            }

            return clone;
        }
    }
}
