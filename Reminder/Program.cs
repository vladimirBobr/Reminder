using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventPrinter;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading;
using ReminderApp.EventScheduling;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    private static readonly IDateTimeProvider _dateTimeProvider = new DateTimeProvider();
    private static readonly IEventScheduler _eventScheduler = new EventScheduler();
    private static readonly IFileStorage _fileStorage = new JsonFileStorage();
    private static IEventReader _eventReader = null!;

    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");
        
        // Read GitHub URL from console
        Console.Write("Enter GitHub file URL (e.g., https://github.com/owner/repo/blob/main/events.txt): ");
        var githubUrl = Console.ReadLine() ?? "";
        
        // Read GitHub token from console
        Console.Write("Enter GitHub Personal Access Token: ");
        var githubToken = Console.ReadLine() ?? "";
        
        // Create GitHubEventReader from URL
        try
        {
            _eventReader = GitHubEventReader.FromUrl(githubUrl, githubToken);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return;
        }
        
        var _notifier = new ConsoleNotifier(); //ConsoleNotifier/TelegramNotifier
        var _eventPrinter = new EventPrinter.EventPrinter();

        var _runner = new EventRunner(
            _eventScheduler,
            _dateTimeProvider,
            _fileStorage,
            _eventReader,
            _notifier,
            _eventPrinter);

        await _runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        _runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}