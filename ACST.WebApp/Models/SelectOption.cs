namespace ACST.WebApp.Models;

public class SelectOption<TValue>
{
    public TValue Value { get; set; } = default!;
    public string Text { get; set; } = string.Empty;
    public string? Subtext { get; set; }
    public string? Badge { get; set; }
    public bool Disabled { get; set; }

    public SelectOption() { }

    public SelectOption(TValue value, string text, string? subtext = null, string? badge = null, bool disabled = false)
    {
        Value = value;
        Text = text;
        Subtext = subtext;
        Badge = badge;
        Disabled = disabled;
    }
}

public static class SelectOption
{
    public static SelectOption<TValue> Create<TValue>(TValue value, string text, string? subtext = null, string? badge = null, bool disabled = false)
        => new(value, text, subtext, badge, disabled);
}
