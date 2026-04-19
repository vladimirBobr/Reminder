using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class DailyDigestProcessor : ProcessorBase, IDailyDigestProcessor
{
    private readonly int _digestHour;

    private DateOnly? _lastDigestDate;
    private const string LastDigestKey = "last_digest_date";

    public DailyDigestProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEnumerable<INotifier> notifiers,
        int digestHour = 7)
        : base(dateTimeProvider, fileStorage, notifiers)
    {
        _digestHour = digestHour;
    }

    protected override async Task OnInitializeAsync()
    {
        _lastDigestDate = await LoadLastDigestDateAsync();
    }

    public override async Task SendIfNeededAsync(List<EventData> events, DateTime now)
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
            Log.Information($"📅 {today:dd.MM.yyyy} - нет событий");
            return;
        }

        Log.Information($"📅 {today:dd.MM.yyyy} - найдено {todayEvents.Count} событий, отправляю Digest...");

        var digest = BuildDigestMessage(todayEvents);
        await NotifyAllAsync(digest);
        Log.Information("✅ Daily Digest отправлен");
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