namespace ReminderApp.EventNotification;

public interface INotifier
{
    void Notify(string message);
}