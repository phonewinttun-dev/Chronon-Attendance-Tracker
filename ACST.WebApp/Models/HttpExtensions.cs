using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ACST.WebApp.Models;

public static class HttpExtensions
{
    public static async Task<string> GetFriendlyErrorMessageAsync(this HttpResponseMessage response, string defaultPrefix)
    {
        try
        {
            var errorResult = await response.Content.ReadFromJsonAsync<ApiResult>();
            if (errorResult != null && !string.IsNullOrEmpty(errorResult.Message))
            {
                return $"{defaultPrefix}: {errorResult.Message}";
            }
        }
        catch
        {
            // Ignore parse exception and fallback
        }

        var reason = response.ReasonPhrase;
        if (string.IsNullOrEmpty(reason))
        {
            reason = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Bad Request",
                System.Net.HttpStatusCode.Unauthorized => "Unauthorized",
                System.Net.HttpStatusCode.Forbidden => "Forbidden",
                System.Net.HttpStatusCode.NotFound => "Not Found",
                System.Net.HttpStatusCode.InternalServerError => "Internal Server Error",
                _ => response.StatusCode.ToString()
            };
        }
        return $"{defaultPrefix}: {reason} ({(int)response.StatusCode})";
    }

    public static string GetFriendlyErrorMessage(this Exception ex, string defaultPrefix)
    {
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode.HasValue)
            {
                var code = (int)httpEx.StatusCode.Value;
                var name = httpEx.StatusCode.Value.ToString();
                return $"{defaultPrefix}: {name} ({code})";
            }
        }

        var msg = ex.Message;
        if (msg.Contains("net_http_message_not_success_statuscode_reason"))
        {
            var parts = msg.Split(',');
            if (parts.Length >= 3)
            {
                // parts[1] is the code (e.g. 400), parts[2] is the reason (e.g. Bad Request)
                return $"{defaultPrefix}: {parts[2].Trim()} ({parts[1].Trim()})";
            }
            return $"{defaultPrefix}: Bad Request (400)";
        }

        return $"{defaultPrefix}: {msg}";
    }
}
