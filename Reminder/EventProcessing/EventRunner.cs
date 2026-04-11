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

    // Кэш даты последней отправки Digest
    private DateOnly? _lastDigestDate;

    // Кэш отправленных напоминаний (ключ = "yyyy-MM-dd HH:mm-Subject")
    private readonly HashSet<string> _sentReminders = new();

    // Время отправки Daily Digest
    private const int DigestHour = 7;
    private const string LastDigestKey = "last_digest_date";
    private const string SentRemindersKey = "sent_reminders";

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

        // Загружаем дату последней отправки Digest
        _lastDigestDate = await LoadLastDigestDateAsync();
        Console.WriteLine($"📅 Последний Digest был: {_lastDigestDate?.ToString("dd.MM.yyyy") ?? "никогда"}");

        // Загружаем список отправленных напоминаний
        await LoadSentRemindersAsync();

        Console.WriteLine("▶️ EventRunner запущен. Проверка каждые 5 секунд.");

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
                await CheckAndSendDigestIfNeededAsync();
                await CheckAndSendRemindersIfNeededAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }

            try
            {
                await Task.Delay(5_000, ct); // 5 секунд
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Проверяет и отправляет Daily Digest в 7:00 утра
    /// </summary>
    internal async Task CheckAndSendDigestIfNeededAsync()
    {
        var now = _dateTimeProvider.Now;

        // Проверяем: если уже 7:00 прошло и сегодня ещё не отправляли
        var today = DateOnly.FromDateTime(now);
        if (now.Hour >= DigestHour && _lastDigestDate != today)
        {
            await SendDailyDigestAsync(now);
            _lastDigestDate = today;
            await SaveLastDigestDateAsync(today);
        }
    }

    /// <summary>
    /// Проверяет и отправляет напоминания за час до событий с временем
    /// </summary>
    internal async Task CheckAndSendRemindersIfNeededAsync()
    {
        var now = _dateTimeProvider.Now;
        var today = DateOnly.FromDateTime(now);

        var allEvents = await _eventReader.ReadEventsAsync();

        // Фильтруем: события на сегодня, с указанием времени, время которых через 45-60 минут
        var upcomingEvents = allEvents
            .Where(e => e.Date == today && e.Time.HasValue)
            .ToList();

        foreach (var evt in upcomingEvents)
        {
            var eventTime = evt.Date.ToDateTime(evt.Time!.Value);
            var minutesUntilEvent = (eventTime - now).TotalMinutes;

            // Если до события 45-60 минут (окно для отправки)
            if (minutesUntilEvent >= 45 && minutesUntilEvent <= 60)
            {
                var reminderKey = evt.GetKey();

                // Если ещё не отправляли
                if (!_sentReminders.Contains(reminderKey))
                {
                    await SendReminderAsync(evt, (int)minutesUntilEvent);
                    _sentReminders.Add(reminderKey);
                    await SaveSentRemindersAsync();
                }
            }
        }
    }

    /// <summary>
    /// Отправляет напоминание за час до события
    /// </summary>
    private async Task SendReminderAsync(EventData evt, int minutesUntil)
    {
        var timeStr = evt.Time?.ToString("HH:mm") ?? "";
        var message = $"🔔 Через {minutesUntil} минут: {timeStr} - {evt.Subject}";

        if (!string.IsNullOrEmpty(evt.Description))
        {
            message += $"\n{evt.Description}";
        }

        _notifier.Notify(message);
        Console.WriteLine($"✅ Напоминание отправлено: {evt.Subject}");
    }

    /// <summary>
    /// Загружает список отправленных напоминаний
    /// </summary>
    private async Task LoadSentRemindersAsync()
    {
        try
        {
            var data = await _fileStorage.LoadAsync(SentRemindersKey);
            if (!string.IsNullOrEmpty(data))
            {
                var keys = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var key in keys)
                {
                    _sentReminders.Add(key);
                }
            }
            Console.WriteLine($"📋 Загружено { _sentReminders.Count} напоминаний");
        }
        catch
        {
            // Игнорируем ошибки
        }
    }

    /// <summary>
    /// Сохраняет список отправленных напоминаний
    /// </summary>
    private async Task SaveSentRemindersAsync()
    {
        try
        {
            var data = string.Join(";", _sentReminders);
            await _fileStorage.SaveAsync(SentRemindersKey, data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Не удалось сохранить напоминания: {ex.Message}");
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