
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
    
    // GitHub configuration - replace with your details
    private const string GitHubOwner = "your-username";
    private const string GitHubRepo = "your-repo";
    private const string GitHubFilePath = "events.txt";
    private const string GitHubBranch = "main";
    
    private static readonly IEventReader _eventReader = new GitHubEventReader(
        GitHubOwner,
        GitHubRepo,
        GitHubFilePath,
        GitHubBranch);
    private static readonly INotifier _notifier = new ConsoleNotifier(); //ConsoleNotifier/TelegramNotifier
    private static readonly IEventPrinter _eventPrinter = new EventPrinter.EventPrinter();

    private static readonly IEventRunner _runner = new EventRunner(
        _eventScheduler,
        _dateTimeProvider,
        _fileStorage,
        _eventReader,
        _notifier,
        _eventPrinter);

    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        await _runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        _runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}
