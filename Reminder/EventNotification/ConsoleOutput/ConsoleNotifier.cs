using ReminderApp.EventNotification.Ntfy;

namespace ReminderApp.EventNotification.ConsoleOutput;

public class ConsoleNotifier : INtfyNotifier
{
    public Task NotifyAsync(string message)
    {
        Console.WriteLine($"🔔 УВЕДОМЛЕНИЕ:");
        Console.WriteLine(message);
        Console.WriteLine("---");
        return Task.CompletedTask;
    }
}
