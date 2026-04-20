using System.Collections.Concurrent;
using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing.Processors;
using ReminderApp.EventReading;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing;

public class EventRunner : IEventRunner
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventReader _eventReader;
    private readonly IDailyDigestProcessor _dailyDigestProcessor;
    private readonly IReminderProcessor _reminderProcessor;
    private readonly IWeeklyDigestProcessor _weeklyDigestProcessor;
    private readonly ICurrentWeekDigestProcessor _currentWeekDigestProcessor;
    private readonly IEventOutputPrinter _printer;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;
    private List<EventData> _events;

    public EventRunner(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEventReader eventReader,
        IEventOutputPrinter eventPrinter,
        IDailyDigestProcessor dailyDigestProcessor,
        IReminderProcessor reminderProcessor,
        IWeeklyDigestProcessor weeklyDigestProcessor,
        ICurrentWeekDigestProcessor currentWeekDigestProcessor,
        IEventOutputPrinter printer)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventReader = eventReader;
        _dailyDigestProcessor = dailyDigestProcessor;
        _reminderProcessor = reminderProcessor;
        _weeklyDigestProcessor = weeklyDigestProcessor;
        _currentWeekDigestProcessor = currentWeekDigestProcessor;
        _printer = printer;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        var intervalSec = DebugHelper.LoopDelayMs / 1000;
        Log.Information($"▶️ EventRunner запущен. Проверка каждые {intervalSec} сек.");

        _ = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _isRunning = false;
        Log.Information("⏸️ EventRunner остановлен.");
    }

    internal void SendDigest()
    {
        _dailyDigestProcessor.SendDailyDigestAsync(_events, _dateTimeProvider.Now);
    }

    internal void SendWeeklyDigest()
    {
        _weeklyDigestProcessor.SendWeeklyDigestAsync(_events, _dateTimeProvider.Now);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Читаем события из источника
                _events = await _eventReader.ReadEventsAsync();
                _printer.PrintEvents(_events);
                var now = _dateTimeProvider.Now;

                // Вызываем обработчики
                await _dailyDigestProcessor.SendIfNeededAsync(_events, now);
                await _reminderProcessor.SendIfNeededAsync(_events, now);
                await _weeklyDigestProcessor.SendIfNeededAsync(_events, now);
                await _currentWeekDigestProcessor.SendIfNeededAsync(_events, now);
            }
            catch (Exception ex)
            {
                Log.Information($"❌ Ошибка: {ex.Message}");
            }

            try
            {
                await Task.Delay(DebugHelper.LoopDelayMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}