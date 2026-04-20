using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventNotification.YandexMail;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Processors;
using ReminderApp.EventReading;
using ReminderApp.EventReading.Debug;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    static async Task Main()
    {
        // Ensure console supports UTF-8 emoji output on Windows
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        Log.Information("▶️ Starting Reminder");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();
        var notifiers = new List<INotifier>();
        IEventReader eventReader;

        if (DebugHelper.IsDebug)
        {
            // В DEBUG используем DebugEventReader и консольный нотификатор
            var consoleNotifier = new ConsoleNotifier();
            await consoleNotifier.NotifyAsync("▶️ Reminder started (DEBUG)");
            
            notifiers.Add(consoleNotifier);
            eventReader = new DebugEventReader();
        }
        else
        {
            // В RELEASE используем GitHub и реальные нотификаторы
            NtfyNotifier ntfyNotifier = new(new NtfyCredentialsProvider());
            await ntfyNotifier.NotifyAsync("▶️ Reminder started");

            notifiers.Add(ntfyNotifier);
            notifiers.Add(new YandexMailNotifier(new YandexMailCredentialsProvider()));
            
            eventReader = new GitHubEventReader(new GitHubCredentialsProvider());
        }

        // Создаём процессоры
        var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, notifiers);
        var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var currentWeekDigestProcessor = new CurrentWeekDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var printer = new EventOutputPrinter(dateTimeProvider);

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            eventReader,
            new EventOutputPrinter(dateTimeProvider),
            dailyDigestProcessor,
            reminderProcessor,
            weeklyDigestProcessor,
            currentWeekDigestProcessor,
            printer);

        AdminApi.Start(runner);

        await runner.StartAsync();

        await Task.Delay(-1);
    }
}