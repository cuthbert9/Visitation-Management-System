using Microsoft.AspNetCore.Components;

namespace VMS.Web.Shared;

public class OverflowMenuItem
{
    public string Label { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public bool IsDestructive { get; set; }
    public EventCallback OnClick { get; set; }
}
