using ReminderApp.Common;
using ReminderApp.EventNotification;

namespace Reminder.Tests.EventProcessing.Helpers;

public class ThrowingNotifier : INotifier
{
    public void Notify(EventData eventData)
    {
        throw new InvalidOperationException("Simulated notification failure");
    }
}
