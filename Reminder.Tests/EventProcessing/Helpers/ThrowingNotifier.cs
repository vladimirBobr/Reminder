using ReminderApp.EventNotification;
using ReminderApp.Events;

namespace Reminder.Tests.EventProcessing.Helpers;

public class ThrowingNotifier : INotifier
{
    public void Notify(EventData eventData)
    {
        throw new InvalidOperationException("Simulated notification failure");
    }
}
