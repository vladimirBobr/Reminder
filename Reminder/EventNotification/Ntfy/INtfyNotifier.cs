namespace ReminderApp.EventNotification.Ntfy;

public interface INtfyNotifier
{
    Task NotifyAsync(string message);
}