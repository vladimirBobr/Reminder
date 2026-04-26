using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventNotification.YandexMail;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Processors;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;
using ReminderApp.GitHubApi;

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
        
        // В DEBUG режиме используем заглушку для консоли, в RELEASE - Ntfy
        INtfyNotifier notifier;
        if (DebugHelper.IsDebug)
        {
            notifier = new ConsoleNotifier();
            Log.Information("🔧 DEBUG MODE: используется ConsoleNotifier (без отправки в Ntfy)");
        }
        else
        {
            notifier = new NtfyNotifier(new NtfyCredentialsProvider());
        }
        
        await notifier.NotifyAsync("▶️ Reminder started");

        // Создаём процессоры - каждый получает notifier напрямую
        var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, notifier);
        var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, notifier);
        var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifier);
        var twoWeekDigestProcessor = new TwoWeekDigestProcessor(dateTimeProvider, fileStorage, notifier);
        var printer = new EventOutputPrinter(dateTimeProvider);

        var gitHubClient = new GitHubClient(new GitHubCredentialsProvider());
        var eventReader = new GitHubEventReader(gitHubClient);

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            eventReader,
            new EventOutputPrinter(dateTimeProvider),
            dailyDigestProcessor,
            reminderProcessor,
            weeklyDigestProcessor,
            twoWeekDigestProcessor,
            printer);

        AdminApi.Start(runner, gitHubClient);

        await runner.StartAsync();

        await Task.Delay(-1);
    }
}
