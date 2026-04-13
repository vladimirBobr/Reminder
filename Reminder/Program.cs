using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.SmsAero;
using ReminderApp.EventNotification.SmsRu;
using ReminderApp.EventNotification.Telegram;
using ReminderApp.EventNotification.YandexMail;
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
        var notifier = new ConsoleNotifier(); // или SmsRuNotifier, TelegramNotifier, YandexMailNotifier

        // Создаём отправителей
        var digestSender = new DigestSender(dateTimeProvider, fileStorage, notifier);
        var reminderSender = new ReminderSender(dateTimeProvider, fileStorage, notifier);

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            new GitHubEventReader(new GitHubCredentialsProvider()),
            notifier,
            new EventOutputPrinter(),
            digestSender,
            reminderSender);

        await runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}