namespace ACST.WebApp.Services;

/// <summary>
/// Coordinates open dropdowns, popups, and date pickers to ensure only one is open at a time across components.
/// </summary>
public static class DropdownCoordinator
{
    public static event Action<object>? OnAnyDropdownOpened;

    public static void NotifyOpened(object source)
    {
        OnAnyDropdownOpened?.Invoke(source);
    }
}
