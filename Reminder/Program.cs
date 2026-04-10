using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.SmsAero;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading;
using ReminderApp.EventReading.GitHub;
using ReminderApp.EventScheduling;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        var runner = new EventRunner(
            new EventScheduler(),
            new DateTimeProvider(),
            new JsonFileStorage(),
            new GitHubEventReader(new GitHubCredentialsProvider()),
            new SmsAeroNotifier(new SmsAeroCredentialsProvider()), //new ConsoleNotifier()
            new EventOutputPrinter());

        await runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}
