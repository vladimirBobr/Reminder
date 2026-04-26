using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class TwoWeekDigestProcessor : ITwoWeekDigestProcessor
{
    private static readonly ILogger _log = Log.ForContext<TwoWeekDigestProcessor>();
    private const int DigestHour = 9;
    private const int DaysAhead = 14;
    
    private readonly string _topic;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly INtfyNotifier _ntfy;
    
    private DateOnly? _lastDigestDate;
    private const string LastDigestKey = "last_two_week_digest_date";

    public TwoWeekDigestProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        INtfyNotifier ntfy,
        string topic)
    {
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _ntfy = ntfy;
        _topic = topic;

        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        _lastDigestDate = await LoadLastDigestDateAsync();
    }

    public async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Проверяем: если уже 9:00 и сегодня ещё не отправляли
        if (now.Hour >= DigestHour && _lastDigestDate != today)
        {
            await SendUpcoming14DaysDigestAsync(events, now);
            _lastDigestDate = today;
            await SaveLastDigestDateAsync(today);
        }
    }

    public async Task SendUpcoming14DaysDigestAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var endDate = today.AddDays(DaysAhead);

        // Фильтруем события с текущего дня до +14 дней
        var upcomingEvents = events
            .Where(e => e.Date >= today && e.Date <= endDate)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();

        if (upcomingEvents.Count == 0)
        {
            _log.Information("📅 {Today:dd.MM} - {EndDate:dd.MM} - нет событий", today, endDate);
            return;
        }

        _log.Information("📅 Ближайшие 14 дней ({Today:dd.MM} - {EndDate:dd.MM}): {Count} событий, отправляю Two Week Digest...", today, endDate, upcomingEvents.Count);

        var digest = BuildTwoWeekDigestMessage(today, endDate, upcomingEvents);
        await _ntfy.NotifyAsync(digest, _topic);
        _log.Information("✅ Two Week Digest отправлен");
    }

    private string BuildTwoWeekDigestMessage(DateOnly startDate, DateOnly endDate, List<EventData> events)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📅 Ближайшие 14 дней ({startDate:dd.MM} - {endDate:dd.MM}):");
        sb.AppendLine();

        // Группируем события по дате
        var eventsByDate = events
            .GroupBy(e => e.Date)
            .OrderBy(g => g.Key);

        foreach (var dateGroup in eventsByDate)
        {
            var dayName = dateGroup.Key.ToString("dddd", new System.Globalization.CultureInfo("ru-RU"));
            sb.AppendLine();
            sb.AppendLine($"{dayName.ToUpper()} #{dateGroup.Key:dd.MM.yyyy}#");
            
            foreach (var e in dateGroup.OrderBy(ev => ev.Time ?? TimeOnly.MaxValue))
            {
                var timeStr = e.Time?.ToString("HH:mm");
                var prefix = timeStr != null ? $"    • {timeStr} " : "    • ";
                sb.AppendLine($"{prefix}{e.Subject}");

                if (!string.IsNullOrEmpty(e.Description))
                {
                    foreach (var descLine in e.Description.Split('\n'))
                    {
                        sb.AppendLine($"      {descLine.Trim()}");
                    }
                }
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

public interface ITwoWeekDigestProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
    Task SendUpcoming14DaysDigestAsync(List<EventData> events, DateTime now);
}