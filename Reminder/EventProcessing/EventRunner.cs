using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventReading;
using ReminderApp.EventScheduling;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing;

public class EventRunner : IEventRunner
{
    private readonly IEventScheduler _scheduler;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IEventReader _eventReader;
    private readonly INotifier _notifier;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    private const string ProcessedFilePath = "processed.json";

    public EventRunner(
        IEventScheduler scheduler,
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEventReader eventReader,
        INotifier notifier)
    {
        _scheduler = scheduler;
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _eventReader = eventReader;
        _notifier = notifier;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        _ = RunLoopAsync(_cts.Token);

        Console.WriteLine("▶️ EventRunner запущен. Проверка каждые 5 секунд.");
    }

    public void Stop()
    {
        _cts?.Cancel();
        _isRunning = false;
        Console.WriteLine("⏸️ EventRunner остановлен.");
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckAndNotifyAsync(ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в цикле: {ex.Message}");
            }

            try
            {
                await Task.Delay(5000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task CheckAndNotifyAsync(CancellationToken ct)
    {
        var processed = await _fileStorage.LoadProcessedAsync(ProcessedFilePath);

        var now = _dateTimeProvider.Now;
        var events = await _eventReader.ReadEventsAsync();
        var dueEvents = _scheduler.GetDueEvents(events, processed, now);

        foreach (var eventData in dueEvents)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                _notifier.Notify(eventData);

                var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
                processed[notifyKey] = now;

                await _fileStorage.SaveProcessedAsync(ProcessedFilePath, processed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при отправке уведомления: {ex.Message}");
            }
        }
    }
}
