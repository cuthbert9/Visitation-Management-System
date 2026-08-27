namespace VMS.Web.Shared;

public class TimelineEntry
{
    public string Title { get; set; } = string.Empty;
    public string? By { get; set; }
    public string? Role { get; set; }
    public DateTime? At { get; set; }
    public string? Comment { get; set; }
    public string Tone { get; set; } = "gray";
    public bool IsPending { get; set; }
}
