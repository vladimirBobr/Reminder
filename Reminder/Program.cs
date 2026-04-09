using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
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

    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        var eventReader = new GitHubEventReaderFactory(_configStorage).Create();
        if (eventReader == null)
            return;

        // Initialize SMSAero notifier with credentials provider (asks from console or loads from encrypted file)
        var credentialsProvider = new SmsAeroCredentialsProvider();
        var notifier = new SmsAeroNotifier(credentialsProvider);

        var eventPrinter = new EventPrinter.EventPrinter();

        var runner = new EventRunner(
            _eventScheduler,
            _dateTimeProvider,
            _fileStorage,
            eventReader,
            notifier,
            eventPrinter);

        await runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }
}
