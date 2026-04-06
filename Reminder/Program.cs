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
    private static readonly GitHubConfigStorage _configStorage = new GitHubConfigStorage();
    private static IEventReader _eventReader = null!;

    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");
        
        // Try to load saved config
        var config = _configStorage.Load();
        
        string githubUrl;
        string githubToken;
        
        if (config != null)
        {
            // Ask if user wants to use saved config
            Console.WriteLine($"Saved URL found: {config.GithubUrl}");
            Console.Write("Use saved credentials? (y/n): ");
            var useSaved = Console.ReadLine()?.ToLower() == "y";
            
            if (useSaved)
            {
                githubUrl = config.GithubUrl;
                githubToken = _configStorage.GetDecryptedToken(config.EncryptedToken) ?? "";
                
                if (string.IsNullOrEmpty(githubToken))
                {
                    Console.WriteLine("❌ Failed to decrypt saved token. Please enter new credentials.");
                    Console.Write("Enter GitHub file URL: ");
                    githubUrl = Console.ReadLine() ?? "";
                    Console.Write("Enter GitHub Personal Access Token: ");
                    githubToken = Console.ReadLine() ?? "";
                    _configStorage.Save(githubUrl, githubToken);
                }
            }
            else
            {
                Console.Write("Enter GitHub file URL: ");
                githubUrl = Console.ReadLine() ?? "";
                Console.Write("Enter GitHub Personal Access Token: ");
                githubToken = Console.ReadLine() ?? "";
                
                Console.Write("Save credentials? (y/n): ");
                var save = Console.ReadLine()?.ToLower() == "y";
                if (save)
                {
                    _configStorage.Save(githubUrl, githubToken);
                    Console.WriteLine("✅ Credentials saved.");
                }
            }
        }
        else
        {
            // First time - ask for credentials
            Console.Write("Enter GitHub file URL (e.g., https://github.com/owner/repo/blob/main/events.txt): ");
            githubUrl = Console.ReadLine() ?? "";
            Console.Write("Enter GitHub Personal Access Token: ");
            githubToken = Console.ReadLine() ?? "";
            
            Console.Write("Save credentials? (y/n): ");
            var save = Console.ReadLine()?.ToLower() == "y";
            if (save)
            {
                _configStorage.Save(githubUrl, githubToken);
                Console.WriteLine("✅ Credentials saved.");
            }
        }
        
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