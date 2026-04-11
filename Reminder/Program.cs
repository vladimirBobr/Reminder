using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.SmsAero;
using ReminderApp.EventNotification.SmsRu;
using ReminderApp.EventNotification.Telegram;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        var runner = new EventRunner(
            new DateTimeProvider(),
            new JsonFileStorage(),
            new GitHubEventReader(new GitHubCredentialsProvider()),
            new ConsoleNotifier(),
            //new SmsRuNotifier(new SmsRuCredentialsProvider()),
            //new TelegramNotifier(new TelegramCredentialsProvider()),
            //new SmsAeroNotifier(new SmsAeroCredentialsProvider()),
            new EventOutputPrinter());

        await runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}
