using Microsoft.JSInterop;

namespace ACST.WebApp.Services;

public class ToastService : IToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<string> Success(string title, string? description = null, int? duration = null)
    {
        try
        {
            return await _js.InvokeAsync<string>("chrononDing.show", "success", title, description, duration ?? 3500);
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    public async ValueTask<string> Error(string title, string? description = null, int? duration = null)
    {
        try
        {
            return await _js.InvokeAsync<string>("chrononDing.show", "error", title, description, duration ?? 5000);
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    public async ValueTask<string> Warning(string title, string? description = null, int? duration = null)
    {
        try
        {
            return await _js.InvokeAsync<string>("chrononDing.show", "warning", title, description, duration ?? 5000);
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    public async ValueTask<string> Info(string title, string? description = null, int? duration = null)
    {
        try
        {
            return await _js.InvokeAsync<string>("chrononDing.show", "info", title, description, duration ?? 3500);
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    public async ValueTask<string> Loading(string title, string? description = null)
    {
        try
        {
            return await _js.InvokeAsync<string>("chrononDing.show", "loading", title, description, 0);
        }
        catch (JSException)
        {
            return string.Empty;
        }
    }

    public async ValueTask Update(string id, string type, string title, string? description = null)
    {
        try
        {
            await _js.InvokeVoidAsync("chrononDing.update", id, type, title, description);
        }
        catch (JSException)
        {
            // Safeguard against disposal race conditions
        }
    }

    public async ValueTask Dismiss(string? id = null)
    {
        try
        {
            await _js.InvokeVoidAsync("chrononDing.dismiss", id);
        }
        catch (JSException)
        {
            // Safeguard against disposal race conditions
        }
    }
}
