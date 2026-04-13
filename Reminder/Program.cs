using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.YandexMail;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Senders;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;
using Serilog;

namespace ReminderApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        Log.Information("▶️ Starting Reminder");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();
        var notifier = new YandexMailNotifier(new YandexMailCredentialsProvider()); // или SmsRuNotifier, TelegramNotifier, YandexMailNotifier

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
        await Task.Delay(-1);
    }
}
