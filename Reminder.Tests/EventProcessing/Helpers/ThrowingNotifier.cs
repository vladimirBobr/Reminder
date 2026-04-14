using ReminderApp.EventNotification;

namespace Reminder.Tests.EventProcessing.Helpers;

public class ThrowingNotifier : INotifier
{
    public Task NotifyAsync(string message)
    {
        throw new InvalidOperationException("Simulated notification failure");
    }
}