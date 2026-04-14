namespace ReminderApp.EventNotification;

public interface INotifier
{
    Task NotifyAsync(string message);
}
