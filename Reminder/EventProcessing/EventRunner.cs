using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing.Senders;
using ReminderApp.EventReading;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing;

public class EventRunner : IEventRunner
{
    // В Debug - 5 секунд, в Release - 60 секунд
    private const int LoopDelayMs =
#if DEBUG
        5_000;
#else
        60_000;
#endif

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventReader _eventReader;
    private readonly IDigestSender _digestSender;
    private readonly IReminderSender _reminderSender;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    public EventRunner(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEventReader eventReader,
        INotifier notifier,
        IEventOutputPrinter eventPrinter,
        IDigestSender digestSender,
        IReminderSender reminderSender)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventReader = eventReader;
        _digestSender = digestSender;
        _reminderSender = reminderSender;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        // Инициализируем отправителей - загружаем состояние
        await _digestSender.InitializeAsync();
        await _reminderSender.InitializeAsync();

        var intervalSec = LoopDelayMs / 1000;
        Console.WriteLine($"▶️ EventRunner запущен. Проверка каждые {intervalSec} сек.");

        _ = RunLoopAsync(_cts.Token);
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
                // Читаем события из источника
                var events = await _eventReader.ReadEventsAsync();
                var now = _dateTimeProvider.Now;

                // Вызываем обработчики
                await _digestSender.SendIfNeededAsync(events, now);
                await _reminderSender.SendIfNeededAsync(events, now);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }

            try
            {
                await Task.Delay(LoopDelayMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
