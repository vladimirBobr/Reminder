using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.SmsAero;
using ReminderApp.EventNotification.SmsRu;
using ReminderApp.EventNotification.Telegram;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Senders;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();
        var notifier = new ConsoleNotifier(); // или SmsRuNotifier, TelegramNotifier

        // Создаём отправителей
        var digestSender = new DigestSender(dateTimeProvider, fileStorage, notifier);
        var reminderScheduler = new ReminderScheduler(dateTimeProvider, fileStorage, notifier);

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            new GitHubEventReader(new GitHubCredentialsProvider()),
            notifier,
            new EventOutputPrinter(),
            digestSender,
            reminderScheduler);

        await runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}