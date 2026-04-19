using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class WeeklyDigestProcessor : ProcessorBase, IWeeklyDigestProcessor
{
    // Пятница 18:00 и Воскресенье 20:00
    private static readonly (DayOfWeek Day, int Hour)[] Schedule =
    [
        (DayOfWeek.Friday, 18),
        (DayOfWeek.Sunday, 20)
    ];

    private int? _lastSentWeek;
    private int? _lastSentYear;
    private const string LastSentWeekKey = "last_weekly_digest_week";

    public WeeklyDigestProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEnumerable<INotifier> notifiers)
        : base(dateTimeProvider, fileStorage, notifiers)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        (_lastSentYear, _lastSentWeek) = await LoadLastSentWeekAsync();
    }

    public override async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        // Получаем текущую неделю (по ISO: неделя начинается с понедельника)
        var (year, week) = GetIsoWeek(now);

        // Если уже отправляли на этой неделе - не отправляем
        if (_lastSentYear == year && _lastSentWeek == week)
            return;

        // Проверяем, подходит ли текущее время
        var shouldSend = Schedule.Any(s => s.Day == now.DayOfWeek && now.Hour == s.Hour);

        if (shouldSend)
        {
            await SendWeeklyDigestAsync(events, now);
            _lastSentYear = year;
            _lastSentWeek = week;
            await SaveLastSentWeekAsync(year, week);
        }
    }

    public async Task SendWeeklyDigestAsync(List<EventData> events, DateTime now)
    {
        // События на следующую неделю (пн-пт)
        var nextWeekStart = GetNextWeekMonday(now);
        var nextWeekEnd = nextWeekStart.AddDays(4); // пятница

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

    private static (int Year, int Week) GetIsoWeek(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
        return (date.Year, week);
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

    private async Task<(int Year, int Week)> LoadLastSentWeekAsync()
    {
        var data = await LoadAsync(LastSentWeekKey);
        if (string.IsNullOrEmpty(data))
            return (0, 0);

        var parts = data.Split('-');
        if (parts.Length != 2)
            return (0, 0);

        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private async Task SaveLastSentWeekAsync(int year, int week)
    {
        await SaveAsync(LastSentWeekKey, $"{year}-{week}");
    }
}

public interface IWeeklyDigestProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
    Task SendWeeklyDigestAsync(List<EventData> events, DateTime now);
}