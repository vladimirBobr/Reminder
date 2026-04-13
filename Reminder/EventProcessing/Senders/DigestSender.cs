using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Senders;

public class DigestSender : IDigestSender
{
    private readonly IFileStorage _fileStorage;
    private readonly INotifier _notifier;
    private readonly int _digestHour;

    private DateOnly? _lastDigestDate;
    private const string LastDigestKey = "last_digest_date";

    public DigestSender(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        INotifier notifier,
        int digestHour = 7)
    {
        _fileStorage = fileStorage;
        _notifier = notifier;
        _digestHour = digestHour;
    }

    /// <summary>
    /// Загружает дату последней отправки (вызывается при старте)
    /// </summary>
    public async Task InitializeAsync()
    {
        _lastDigestDate = await LoadLastDigestDateAsync();
    }

    public async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Проверяем: если уже время Digest прошло и сегодня ещё не отправляли
        if (now.Hour >= _digestHour && _lastDigestDate != today)
        {
            await SendDigestAsync(events, now);
            _lastDigestDate = today;
            await SaveLastDigestDateAsync(today);
        }
    }

    private async Task SendDigestAsync(List<EventData> events, DateTime now)
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
        _notifier.Notify(digest);
        Log.Information("✅ Digest отправлен");
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

    private async Task SaveLastDigestDateAsync(DateOnly date)
    {
        try
        {
            await _fileStorage.SaveAsync(LastDigestKey, date.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Не удалось сохранить дату Digest: {ex.Message}");
        }
    }
}
