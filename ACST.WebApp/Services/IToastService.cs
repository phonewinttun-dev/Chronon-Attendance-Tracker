namespace ACST.WebApp.Services;

public interface IToastService
{
    ValueTask<string> Success(string title, string? description = null, int? duration = null);
    ValueTask<string> Error(string title, string? description = null, int? duration = null);
    ValueTask<string> Warning(string title, string? description = null, int? duration = null);
    ValueTask<string> Info(string title, string? description = null, int? duration = null);
    ValueTask<string> Loading(string title, string? description = null);
    ValueTask Update(string id, string type, string title, string? description = null);
    ValueTask Dismiss(string? id = null);
}
