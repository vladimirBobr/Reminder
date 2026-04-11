using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventOutput;
using ReminderApp.EventReading;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing;

public class EventRunner : IEventRunner
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly IEventReader _eventReader;
    private readonly INotifier _notifier;
    private readonly IEventOutputPrinter _eventPrinter;
    private CancellationTokenSource? _cts;
    private bool _isRunning = false;

    // Кэш даты последней отправки Digest (чтобы не читать файл каждый раз)
    private DateOnly? _lastDigestDate;

    // Время отправки Daily Digest (по умолчанию 7 утра)
    private const int DigestHour = 7;
    private const string LastDigestKey = "last_digest_date";

    public EventRunner(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEventReader eventReader,
        INotifier notifier,
        IEventOutputPrinter eventPrinter)
    {
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _eventReader = eventReader;
        _notifier = notifier;
        _eventPrinter = eventPrinter;
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        // Загружаем дату последней отправки только один раз при старте
        _lastDigestDate = await LoadLastDigestDateAsync();
        Console.WriteLine($"📅 Последний Digest был: {_lastDigestDate?.ToString("dd.MM.yyyy") ?? "никогда"}");

        Console.WriteLine("▶️ EventRunner запущен. Проверка каждую минуту.");

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
                Console.WriteLine($"Проверка ..");

                await CheckAndSendDigestIfNeededAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }

            try
            {
                await Task.Delay(5_000, ct); // 1 минута
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Проверяет, если уже 7:00 утра и Digest ещё не отправлялся сегодня - отправляет
    /// </summary>
    internal async Task CheckAndSendDigestIfNeededAsync()
    {
        var now = _dateTimeProvider.Now;

        // Проверяем: если уже 7:00 прошло и сегодня ещё не отправляли
        var today = DateOnly.FromDateTime(now);
        if (now.Hour >= DigestHour && _lastDigestDate != today)
        {
            await SendDailyDigestAsync(now);
            _lastDigestDate = today; // Обновляем кэш в памяти
            await SaveLastDigestDateAsync(today); // Сохраняем в файл
        }
    }

    /// <summary>
    /// Загружает дату последней отправки Digest
    /// </summary>
    private async Task<DateOnly?> LoadLastDigestDateAsync()
    {
        try
        {
            var data = await _fileStorage.LoadAsync(LastDigestKey);
            if (string.IsNullOrEmpty(data))
                return null;

            return DateOnly.Parse(data);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Сохраняет дату последней отправки Digest
    /// </summary>
    private async Task SaveLastDigestDateAsync(DateOnly date)
    {
        try
        {
            await _fileStorage.SaveAsync(LastDigestKey, date.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Не удалось сохранить дату Digest: {ex.Message}");
        }
    }

    /// <summary>
    /// Отправляет Digest со всеми событиями на сегодня
    /// </summary>
    internal async Task SendDailyDigestAsync(DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        var allEvents = await _eventReader.ReadEventsAsync();

        // Фильтруем только события на сегодня
        var todayEvents = allEvents
            .Where(e => e.Date == today)
            .OrderBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();

        if (todayEvents.Count == 0)
        {
            Console.WriteLine($"📅 {today:dd.MM.yyyy} - нет событий");
            return;
        }

        Console.WriteLine($"📅 {today:dd.MM.yyyy} - найдено {todayEvents.Count} событий, отправляю Digest...");

        // Выводим в консоль
        //_eventPrinter.PrintEvents(todayEvents);

        // Формируем и отправляемDigest
        var digest = BuildDigestMessage(todayEvents);
        _notifier.Notify(digest);
        Console.WriteLine("✅ Digest отправлен");
    }

    /// <summary>
    /// Формирует текстовое сообщение со всеми событиями
    /// </summary>
    private string BuildDigestMessage(List<EventData> events)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("События на сегодня:");
        sb.AppendLine();

        foreach (var e in events)
        {
            var timeStr = e.Time?.ToString("HH:mm") ?? "---";
            sb.AppendLine($"• {timeStr} - {e.Subject}");

            if (!string.IsNullOrEmpty(e.Description))
            {
                sb.AppendLine($"  {e.Description}");
            }
        }

        return sb.ToString();
    }
}
