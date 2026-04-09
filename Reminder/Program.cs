using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.SmsAero;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading;
using ReminderApp.EventReading.GitHub;
using ReminderApp.EventScheduling;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    private static readonly IDateTimeProvider _dateTimeProvider = new DateTimeProvider();
    private static readonly IEventScheduler _eventScheduler = new EventScheduler();
    private static readonly IFileStorage _fileStorage = new JsonFileStorage();

    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        // Initialize GitHub event reader with credentials provider (asks from console or loads from encrypted file)
        var githubCredentialsProvider = new GitHubCredentialsProvider();
        var eventReader = new GitHubEventReader(githubCredentialsProvider);

        // Initialize SMSAero notifier with credentials provider (asks from console or loads from encrypted file)
        var smsAeroCredentialsProvider = new SmsAeroCredentialsProvider();
        var notifier = new SmsAeroNotifier(smsAeroCredentialsProvider);

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
