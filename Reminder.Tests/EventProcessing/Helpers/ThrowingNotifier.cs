namespace Reminder.Tests.EventProcessing.Helpers;

public class ThrowingNotifier
{
    public Task NotifyAsync(string message)
    {
        throw new InvalidOperationException("Simulated notification failure");
    }
}