using ReminderApp.EventNotification.Ntfy;

namespace Reminder.Tests.EventProcessing.Helpers;

public class TestNtfyNotifier : INtfyNotifier
{
    public string? LastNotifiedMessage { get; private set; }
    public List<string> NotifiedMessages { get; } = new();

    public Task NotifyAsync(string message, string topic)
    {
        LastNotifiedMessage = message;
        NotifiedMessages.Add(message);
        return Task.CompletedTask;
    }
}