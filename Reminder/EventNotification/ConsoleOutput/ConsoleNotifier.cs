using ReminderApp.Common;

namespace ReminderApp.EventNotification.ConsoleOutput;

public class ConsoleNotifier : INotifier
{
    public void Notify(EventData eventData)
    {
        Console.WriteLine($"🔔 УВЕДОМЛЕНИЕ: {eventData.Subject}");
        Console.WriteLine($"📝 {eventData.Description}");
        Console.WriteLine($"📅 Время: {eventData.Time:dd.MM.yyyy HH:mm}");
        Console.WriteLine("---");
    }
}
