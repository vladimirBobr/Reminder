using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class DailyDigestProcessor : IDailyDigestProcessor
{
    private static readonly ILogger _log = Log.ForContext<DailyDigestProcessor>();
    private readonly string _topic;
    private readonly int _digestHour;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly INtfyNotifier _ntfy;

    private DateOnly? _lastDigestDate;
    private const string LastDigestKey = "last_digest_date";

    public DailyDigestProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        INtfyNotifier ntfy,
        string topic,
        int digestHour = 7)
    {
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _ntfy = ntfy;
        _topic = topic;
        _digestHour = digestHour;

        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        _lastDigestDate = await LoadLastDigestDateAsync();
    }

    public async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Проверяем: если уже время Digest прошло и сегодня ещё не отправляли
        if (now.Hour >= _digestHour && _lastDigestDate != today)
        {
            await SendDailyDigestAsync(events, now);
            _lastDigestDate = today;
            await SaveLastDigestDateAsync(today);
        }
    }

    public async Task SendDailyDigestAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Фильтруем только события на сегодня
        var todayEvents = events
            .Where(e => e.Date == today)
            .OrderBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();

        if (todayEvents.Count == 0)
        {
            _log.Information("📅 {Date:dd.MM.yyyy} - нет событий", today);
            return;
        }

        _log.Information("📅 {Date:dd.MM.yyyy} - найдено {Count} событий, отправляю Digest...", today, todayEvents.Count);

        var digest = BuildDigestMessage(todayEvents);
        await _ntfy.NotifyAsync(digest, _topic);
        _log.Information("✅ Daily Digest отправлен");
    }

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

    private async Task<DateOnly?> LoadLastDigestDateAsync()
    {
        var data = await _fileStorage.LoadStringAsync(LastDigestKey);
        if (string.IsNullOrEmpty(data))
            return null;

        return DateOnly.Parse(data);
    }

    private async Task SaveLastDigestDateAsync(DateOnly date)
    {
        await _fileStorage.SaveStringAsync(LastDigestKey, date.ToString("yyyy-MM-dd"));
    }
}