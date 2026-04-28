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
    private static readonly ILogger _log = Log.ForContext<EventRunner>();
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEventReader _eventReader;
    private readonly IDailyDigestProcessor _dailyDigestProcessor;
    private readonly IReminderProcessor _reminderProcessor;
    private readonly IWeeklyDigestProcessor _weeklyDigestProcessor;
    private readonly ITwoWeekDigestProcessor _twoWeekDigestProcessor;
    private readonly IShopListProcessor _shopListProcessor;
    private readonly IEventOutputPrinter _printer;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;
    private ParsedFileData _parsedData = null!;

    public EventRunner(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEventReader eventReader,
        IEventOutputPrinter eventPrinter,
        IDailyDigestProcessor dailyDigestProcessor,
        IReminderProcessor reminderProcessor,
        IWeeklyDigestProcessor weeklyDigestProcessor,
        ITwoWeekDigestProcessor twoWeekDigestProcessor,
        IShopListProcessor shopListProcessor,
        IEventOutputPrinter printer)
    {
        _dateTimeProvider = dateTimeProvider;
        _eventReader = eventReader;
        _dailyDigestProcessor = dailyDigestProcessor;
        _reminderProcessor = reminderProcessor;
        _weeklyDigestProcessor = weeklyDigestProcessor;
        _twoWeekDigestProcessor = twoWeekDigestProcessor;
        _shopListProcessor = shopListProcessor;
        _printer = printer;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        var intervalSec = DebugHelper.LoopDelayMs / 1000;
        _log.Information("▶️ EventRunner запущен. Проверка каждые {Interval} сек.", intervalSec);

        _ = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _isRunning = false;
        _log.Information("⏸️ EventRunner остановлен.");
    }

    internal void SendDigest()
    {
        _dailyDigestProcessor.SendDailyDigestAsync(_parsedData.Events, _dateTimeProvider.Now);
    }

    internal void SendWeeklyDigest()
    {
        _weeklyDigestProcessor.SendWeeklyDigestAsync(_parsedData.Events, _dateTimeProvider.Now);
    }

    internal void SendTwoWeekDigest()
    {
        _twoWeekDigestProcessor.SendUpcoming14DaysDigestAsync(_parsedData.Events, _dateTimeProvider.Now);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Читаем данные из источника
                _parsedData = await _eventReader.ReadEventsAsync();
                _printer.PrintEvents(_parsedData.Events);
                var now = _dateTimeProvider.Now;

                // Вызываем обработчики
                await _dailyDigestProcessor.SendIfNeededAsync(_parsedData.Events, now);
                await _reminderProcessor.SendIfNeededAsync(_parsedData.Events, now);
                await _weeklyDigestProcessor.SendIfNeededAsync(_parsedData.Events, now);
                await _twoWeekDigestProcessor.SendIfNeededAsync(_parsedData.Events, now);
                await _shopListProcessor.ProcessShoppingListAsync(_parsedData.ShoppingItems, now);
            }
            catch (Exception ex)
            {
                _log.Information("❌ Ошибка: {Error}", ex.Message);
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