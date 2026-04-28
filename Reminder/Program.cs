using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Processors;
using ReminderApp.EventReading;
using ReminderApp.EventReading.Debug;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;
using ReminderApp.GitHubApi;

namespace ReminderApp;

internal class Program
{
    private static ILogger? _log;

    static async Task Main()
    {
        // Ensure console supports UTF-8 emoji output on Windows
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        _log = ConfigureLogger();

        _log.Information("▶ Starting Reminder");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();
        
        // В DEBUG режиме используем заглушку для консоли, в RELEASE - Ntfy
        INtfyNotifier notifier;
        if (DebugHelper.IsDebug)
        {
            notifier = new ConsoleNotifier();
            _log.Information("DEBUG MODE: используется ConsoleNotifier (без отправки в Ntfy)");
        }
        else
        {
            notifier = new NtfyNotifier(new NtfyCredentialsProvider());
        }

        var localIp = GetLocalIpAddress();
        
        await notifier.NotifyAsync($"""
            ▶ Reminder started
            Admin API: http://{localIp}:5000
            Seq: http://{localIp}:5341
            """, NtfyTopics.Reminders);

        var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.DailyDigest);
        var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.Reminders);
        var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.WeeklyDigest);
        var twoWeekDigestProcessor = new TwoWeekDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.TwoWeekDigest);
        var shopListProcessor = new ShopListProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.Shopping);
        var printer = new EventOutputPrinter(dateTimeProvider);

        IEventReader eventReader;
        IGitHubClient? gitHubClient = null;
        if (DebugHelper.IsDebug)
        {
            eventReader = new DebugEventReader();
            _log.Information("DEBUG MODE: используется DebugEventReader (без чтения из GitHub)");
        }
        else
        {
            gitHubClient = new GitHubClient(new GitHubCredentialsProvider());
            eventReader = new GitHubEventReader(gitHubClient);
        }

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            eventReader,
            new EventOutputPrinter(dateTimeProvider),
            dailyDigestProcessor,
            reminderProcessor,
            weeklyDigestProcessor,
            twoWeekDigestProcessor,
            shopListProcessor,
            printer);

        AdminApi.Start(runner, gitHubClient);

        await runner.StartAsync();

        await Task.Delay(-1);
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
            return host.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "localhost";
        }
        catch
        {
            return "localhost";
        }
    }

    private static ILogger ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "Reminder")
            .Enrich.With<ShortClassNameEnricher>()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{ClassName,-20}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        return Log.ForContext<Program>();
    }
}