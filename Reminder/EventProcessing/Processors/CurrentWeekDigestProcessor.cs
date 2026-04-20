using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class CurrentWeekDigestProcessor : ProcessorBase, ICurrentWeekDigestProcessor
{
    private const int DigestHour = 9;
    private DateOnly? _lastDigestDate;
    private const string LastDigestKey = "last_current_week_digest_date";

    public CurrentWeekDigestProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEnumerable<INotifier> notifiers)
        : base(dateTimeProvider, fileStorage, notifiers)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        _lastDigestDate = await LoadLastDigestDateAsync();
    }

    public override async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Проверяем: если уже 9:00 и сегодня ещё не отправляли
        if (now.Hour >= DigestHour && _lastDigestDate != today)
        {
            await SendCurrentWeekDigestAsync(events, now);
            _lastDigestDate = today;
            await SaveLastDigestDateAsync(today);
        }
    }

    public async Task SendCurrentWeekDigestAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var weekEnd = GetEndOfWeek(today);

        // Фильтруем события с текущего дня до конца недели
        var weekEvents = events
            .Where(e => e.Date >= today && e.Date <= weekEnd)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();

        if (weekEvents.Count == 0)
        {
            Log.Information($"📅 {today:dd.MM} - {weekEnd:dd.MM} - нет событий");
            return;
        }

        Log.Information($"📅 Текущая неделя {today:dd.MM} - {weekEnd:dd.MM}: {weekEvents.Count} событий, отправляю Current Week Digest...");

        var digest = BuildCurrentWeekDigestMessage(today, weekEnd, weekEvents);
        await NotifyAllAsync(digest);
        Log.Information("✅ Current Week Digest отправлен");
    }

    private static DateOnly GetEndOfWeek(DateOnly date)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilSunday);
    }

    private string BuildCurrentWeekDigestMessage(DateOnly weekStart, DateOnly weekEnd, List<EventData> events)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📅 Текущая неделя ({weekStart:dd.MM} - {weekEnd:dd.MM}):");
        sb.AppendLine();

        foreach (var e in events)
        {
            var dayName = e.Date.ToString("dddd", new System.Globalization.CultureInfo("ru-RU"));
            var timeStr = e.Time?.ToString("HH:mm") ?? "---";
            sb.AppendLine($"• {e.Date:dd.MM} ({dayName}) {timeStr} - {e.Subject}");

            if (!string.IsNullOrEmpty(e.Description))
            {
                sb.AppendLine($"  {e.Description}");
            }
        }

        return sb.ToString();
    }

    private async Task<DateOnly?> LoadLastDigestDateAsync()
    {
        var data = await LoadAsync(LastDigestKey);
        if (string.IsNullOrEmpty(data))
            return null;

        return DateOnly.Parse(data);
    }

    private async Task SaveLastDigestDateAsync(DateOnly date)
    {
        await SaveAsync(LastDigestKey, date.ToString("yyyy-MM-dd"));
    }
}

public interface ICurrentWeekDigestProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
    Task SendCurrentWeekDigestAsync(List<EventData> events, DateTime now);
}