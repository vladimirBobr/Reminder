namespace ReminderApp.EventNotification.ConsoleOutput;

public class ConsoleNotifier : INotifier
{
    public Task NotifyAsync(string message)
    {
        Console.WriteLine($"🔔 УВЕДОМЛЕНИЕ:");
        Console.WriteLine(message);
        Console.WriteLine("---");
        return Task.CompletedTask;
    }
}