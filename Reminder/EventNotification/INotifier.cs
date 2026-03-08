using ReminderApp.Common;

namespace ReminderApp.EventNotification;

public interface INotifier
{
    void Notify(EventData eventData);
}
