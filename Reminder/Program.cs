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
        
        var (githubUrl, githubToken) = GetGitHubCredentials();
        
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
    
    private static (string url, string token) GetGitHubCredentials()
    {
        var config = _configStorage.Load();
        
        if (config != null)
        {
            return GetCredentialsFromSavedConfig(config);
        }
        
        return GetCredentialsFromInput(savePrompt: true);
    }
    
    private static (string url, string token) GetCredentialsFromSavedConfig(GitHubConfig config)
    {
        Console.WriteLine($"Saved URL found: {config.GithubUrl}");
        Console.Write("Use saved credentials? (y/n): ");
        var useSaved = Console.ReadLine()?.ToLower() == "y";
        
        if (useSaved)
        {
            var token = _configStorage.GetDecryptedToken(config.EncryptedToken) ?? "";
            
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Failed to decrypt saved token. Please enter new credentials.");
                return GetCredentialsFromInput(savePrompt: false);
            }
            
            return (config.GithubUrl, token);
        }
        
        return GetCredentialsFromInput(savePrompt: true);
    }
    
    private static (string url, string token) GetCredentialsFromInput(bool savePrompt)
    {
        Console.Write("Enter GitHub file URL (e.g., https://github.com/owner/repo/blob/main/events.txt): ");
        var url = Console.ReadLine() ?? "";
        Console.Write("Enter GitHub Personal Access Token: ");
        var token = Console.ReadLine() ?? "";
        
        if (savePrompt)
        {
            Console.Write("Save credentials? (y/n): ");
            var save = Console.ReadLine()?.ToLower() == "y";
            if (save)
            {
                _configStorage.Save(url, token);
                Console.WriteLine("✅ Credentials saved.");
            }
        }
        
        return (url, token);
    }
}