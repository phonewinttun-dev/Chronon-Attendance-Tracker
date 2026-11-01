using System.Net;
using System.Text.Json;

namespace ACST.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GlobalExceptionMiddleware] An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            isSuccess = false,
            statusCode = context.Response.StatusCode,
            message = "[GlobalExceptionMiddleware] Internal Server Error from the custom middleware.",
            detailed = _env.IsDevelopment() ? exception.Message : null
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
