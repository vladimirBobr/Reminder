using ReminderApp.EventNotification;

namespace Reminder.Tests.EventProcessing.Helpers;

public class TestNotifier : INotifier
{
    public string? LastNotifiedMessage { get; private set; }
    public List<string> NotifiedMessages { get; } = new();

    public Task NotifyAsync(string message)
    {
        LastNotifiedMessage = message;
        NotifiedMessages.Add(message);
        return Task.CompletedTask;
    }
}