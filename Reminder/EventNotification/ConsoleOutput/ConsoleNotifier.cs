namespace ReminderApp.EventNotification.ConsoleOutput;

public class ConsoleNotifier : INotifier
{
    public void Notify(string message)
    {
        Console.WriteLine($"🔔 УВЕДОМЛЕНИЕ:");
        Console.WriteLine(message);
        Console.WriteLine("---");
    }
}