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
        
        // Parse the URL to extract owner, repo, file path, and branch
        var (owner, repo, filePath, branch) = ParseGitHubUrl(githubUrl);
        
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
        {
            Console.WriteLine("❌ Invalid GitHub URL. Format: https://github.com/owner/repo/blob/branch/filename");
            return;
        }
        
        Console.WriteLine($"📂 Reading from: {owner}/{repo}/{filePath} (branch: {branch})");
        
        // Read GitHub token from console
        Console.Write("Enter GitHub Personal Access Token (press Enter to skip): ");
        var githubToken = Console.ReadLine();
        
        // Create GitHubEventReader
        _eventReader = new GitHubEventReader(owner, repo, filePath, branch);
        
        // Set authentication if token provided
        if (!string.IsNullOrWhiteSpace(githubToken))
        {
            ((GitHubEventReader)_eventReader).SetAuthentication(githubToken);
            Console.WriteLine("🔐 Authentication enabled");
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

    private static (string owner, string repo, string filePath, string branch) ParseGitHubUrl(string url)
    {
        // Handle formats:
        // https://github.com/owner/repo/blob/main/events.txt
        // https://github.com/owner/repo/blob/master/path/to/file.txt
        // owner/repo (shorthand)
        
        if (string.IsNullOrWhiteSpace(url))
            return (null!, null!, "events.txt", "main");
            
        // Check for shorthand format (owner/repo)
        var parts = url.Split('/');
        if (parts.Length >= 2 && !url.Contains("github.com"))
        {
            var owner = parts[0].Trim();
            var repo = parts[1].Trim();
            var filePath = parts.Length > 2 ? string.Join("/", parts.Skip(2)) : "events.txt";
            return (owner, repo, filePath, "main");
        }
        
        // Parse full GitHub URL
        try
        {
            var uri = new Uri(url);
            if (uri.Host != "github.com" && !uri.Host.EndsWith("github.com"))
                return (null!, null!, "events.txt", "main");
                
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            if (pathParts.Length < 2)
                return (null!, null!, "events.txt", "main");
                
            var owner = pathParts[0];
            var repo = pathParts[1];
            
            // Check for /blob/ branch/filepath
            var filePath = "events.txt";
            var branch = "main";
            
            var blobIndex = Array.IndexOf(pathParts, "blob");
            if (blobIndex >= 0 && blobIndex + 2 < pathParts.Length)
            {
                branch = pathParts[blobIndex + 1];
                filePath = string.Join("/", pathParts.Skip(blobIndex + 2));
            }
            
            return (owner, repo, filePath, branch);
        }
        catch
        {
            return (null!, null!, "events.txt", "main");
        }
    }
}