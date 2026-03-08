
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
    private static readonly IEventScheduler _scheduler = new EventScheduler();
    private static readonly IFileStorage _fileStorage = new JsonFileStorage();
    private static readonly IEventReader _eventReader = new EventReader();
    private static readonly INotifier _notifier = new ConsoleNotifier(); //ConsoleNotifier/TelegramNotifier
    private static readonly IEventPrinter _eventPrinter = new EventPrinter.EventPrinter();

    private static readonly IEventRunner _runner = new EventRunner(
        _scheduler,
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
