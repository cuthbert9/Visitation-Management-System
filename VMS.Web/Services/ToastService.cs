namespace VMS.Web.Services;

public enum ToastTone
{
    Success,
    Info,
    Warning,
    Danger
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; init; } = string.Empty;
    public ToastTone Tone { get; init; } = ToastTone.Success;
}

public class ToastService
{
    public event Action? OnChange;

    public List<ToastMessage> Messages { get; } = [];

    public void Show(string message, ToastTone tone = ToastTone.Success)
    {
        var toast = new ToastMessage { Message = message, Tone = tone };
        Messages.Add(toast);
        OnChange?.Invoke();

        _ = DismissAfterDelayAsync(toast.Id);
    }

    public void Dismiss(Guid id)
    {
        Messages.RemoveAll(m => m.Id == id);
        OnChange?.Invoke();
    }

    private async Task DismissAfterDelayAsync(Guid id)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        Dismiss(id);
    }
}
