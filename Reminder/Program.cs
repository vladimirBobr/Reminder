using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventNotification.YandexMail;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Senders;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        Log.Information("▶️ Starting Reminder");

        await new NtfyNotifier(new NtfyCredentialsProvider()).NotifyAsync("▶️ Reminder started");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();

        // Создаём список нотификаторов
        var notifiers = new List<INotifier>
        {
            new NtfyNotifier(new NtfyCredentialsProvider()),
            new YandexMailNotifier(new YandexMailCredentialsProvider()),
            // new TelegramNotifier(new TelegramCredentialsProvider()),
        };

        // Создаём отправителей
        var digestSender = new DigestSender(dateTimeProvider, fileStorage, notifiers);
        var reminderSender = new ReminderSender(dateTimeProvider, fileStorage, notifiers);

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            new GitHubEventReader(new GitHubCredentialsProvider()),
            new EventOutputPrinter(),
            digestSender,
            reminderSender);

        await runner.StartAsync();
        await Task.Delay(-1);
    }
}
