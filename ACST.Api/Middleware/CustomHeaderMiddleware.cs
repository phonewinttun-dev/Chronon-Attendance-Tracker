namespace ACST.Api.Middleware;

public class CustomHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public CustomHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("X-App-Name", "ChrononAttendanceTracker");
            context.Response.Headers.Append("X-Executed-At", DateTime.UtcNow.ToString("O"));
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
