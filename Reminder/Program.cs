using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventNotification.YandexMail;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Processors;
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

        NtfyNotifier ntfyNotifier = new(new NtfyCredentialsProvider());
        await ntfyNotifier.NotifyAsync("▶️ Reminder started");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();

        // Создаём список нотификаторов
        var notifiers = new List<INotifier>
        {
            ntfyNotifier,
            new YandexMailNotifier(new YandexMailCredentialsProvider()),
            // new TelegramNotifier(new TelegramCredentialsProvider()),
        };

        // Создаём процессоры
        var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, notifiers);
        var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var printer = new EventOutputPrinter();

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            new GitHubEventReader(new GitHubCredentialsProvider()),
            new EventOutputPrinter(),
            dailyDigestProcessor,
            reminderProcessor,
            weeklyDigestProcessor,
            printer);

        AdminApi.Start(runner);

        await runner.StartAsync();

        await Task.Delay(-1);
    }
}