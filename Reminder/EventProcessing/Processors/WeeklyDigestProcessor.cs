using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class WeeklyDigestProcessor : ProcessorBase, IWeeklyDigestProcessor
{
    // Пятница 18:00 и Воскресенье 18:00
    private static readonly (DayOfWeek Day, int Hour)[] Schedule =
    [
        (DayOfWeek.Friday, 18),
        (DayOfWeek.Sunday, 18)
    ];

    // Отслеживаем отдельно для каждого слота: "yyyy-MM-dd-HH" (день+час)
    private readonly HashSet<string> _sentSlots = new();
    private const string SentSlotsKey = "last_weekly_digest_slots";

    public WeeklyDigestProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEnumerable<INotifier> notifiers)
        : base(dateTimeProvider, fileStorage, notifiers)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await LoadSentSlotsAsync();
    }

    public override async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        var slotKey = $"{now:yyyy-MM-dd-HH}";

        // Если уже отправляли в этот слот (день+час) - не отправляем
        if (_sentSlots.Contains(slotKey))
            return;

        // Проверяем, подходит ли текущее время
        var scheduleEntry = Schedule.FirstOrDefault(s => s.Day == now.DayOfWeek && s.Hour == now.Hour);

        if (scheduleEntry != default)
        {
            await SendWeeklyDigestAsync(events, now);
            _sentSlots.Add(slotKey);
            await SaveSentSlotsAsync();
        }
    }

    public async Task SendWeeklyDigestAsync(List<EventData> events, DateTime now)
    {
        // События на следующую неделю (пн-вс)
        var nextWeekStart = GetNextWeekMonday(now);
        var nextWeekEnd = nextWeekStart.AddDays(6); // воскресенье

        var weekEvents = events
            .Where(e => e.Date >= nextWeekStart && e.Date <= nextWeekEnd)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();

        if (weekEvents.Count == 0)
        {
            Log.Information($"📅 {nextWeekStart:dd.MM} - {nextWeekEnd:dd.MM} - нет событий");
            return;
        }

        Log.Information($"📅 План на неделю {nextWeekStart:dd.MM} - {nextWeekEnd:dd.MM}: {weekEvents.Count} событий, отправляю Weekly Digest...");

        var digest = BuildWeeklyDigestMessage(nextWeekStart, nextWeekEnd, weekEvents);
        await NotifyAllAsync(digest);
        Log.Information("✅ Weekly Digest отправлен");
    }

    private static DateOnly GetNextWeekMonday(DateTime date)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // если сегодня понедельник, берём следующий
        return DateOnly.FromDateTime(date.AddDays(daysUntilMonday));
    }

    private string BuildWeeklyDigestMessage(DateOnly weekStart, DateOnly weekEnd, List<EventData> events)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📅 План на неделю ({weekStart:dd.MM} - {weekEnd:dd.MM}):");
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

    private async Task LoadSentSlotsAsync()
    {
        var data = await LoadAsync(SentSlotsKey);
        if (string.IsNullOrEmpty(data))
            return;

        var slots = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var slot in slots)
        {
            _sentSlots.Add(slot);
        }
    }

    private async Task SaveSentSlotsAsync()
    {
        var data = string.Join(";", _sentSlots);
        await SaveAsync(SentSlotsKey, data);
    }
}

public interface IWeeklyDigestProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
    Task SendWeeklyDigestAsync(List<EventData> events, DateTime now);
}
