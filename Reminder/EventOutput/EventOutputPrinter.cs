using ReminderApp.Common;
using ReminderApp.DateTimeProviding;

namespace ReminderApp.EventOutput;

public class EventOutputPrinter : IEventOutputPrinter
{
    private readonly HashSet<string> _seenEvents = [];
    private readonly IDateTimeProvider _dateTimeProvider;

    public EventOutputPrinter(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public void PrintEvents(List<EventData> events)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.Now);
        var todayEvents = new List<EventData>();
        var tomorrowEvents = new List<EventData>();
        var nextWeekEvents = new List<EventData>();
        var futureEvents = new List<EventData>();

        // Категоризируем события
        foreach (var e in events)
        {
            var eventKey = e.GetKey();
            if (_seenEvents.Contains(eventKey))
                continue;

            _seenEvents.Add(eventKey);

            var daysDiff = e.Date.DayNumber - today.DayNumber;

            if (daysDiff == 0)
                todayEvents.Add(e);
            else if (daysDiff == 1)
                tomorrowEvents.Add(e);
            else if (daysDiff >= 2 && daysDiff <= 7)
                nextWeekEvents.Add(e);
            else
                futureEvents.Add(e);
        }

        // Выводим с разделителями
        if (todayEvents.Count > 0)
        {
            Log.Information("═══════════════════════════════════════");
            Log.Information("📅 СЕГОДНЯ");
            Log.Information("═══════════════════════════════════════");
            foreach (var e in todayEvents.OrderBy(x => x.Time))
                PrintTodayEvent(e);
        }

        if (tomorrowEvents.Count > 0)
        {
            Log.Information("═══════════════════════════════════════");
            Log.Information("📅 ЗАВТРА");
            Log.Information("═══════════════════════════════════════");
            foreach (var e in tomorrowEvents.OrderBy(x => x.Time))
                PrintTomorrowEvent(e);
        }

        if (nextWeekEvents.Count > 0)
        {
            Log.Information("═══════════════════════════════════════");
            Log.Information("📅 НА СЛЕДУЮЩЕЙ НЕДЕЛЕ");
            Log.Information("═══════════════════════════════════════");
            foreach (var e in nextWeekEvents.OrderBy(x => x.Date).ThenBy(x => x.Time))
                PrintNextWeekEvent(e);
        }

        if (futureEvents.Count > 0)
        {
            Log.Information("═══════════════════════════════════════");
            Log.Information("📅 В БУДУЩЕМ");
            Log.Information("═══════════════════════════════════════");
            foreach (var e in futureEvents.OrderBy(x => x.Date).ThenBy(x => x.Time))
                PrintFutureEvent(e);
        }
    }

    private void PrintTodayEvent(EventData e)
    {
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";
        var hoursUntil = GetHoursUntil(e);
        var hoursStr = hoursUntil.HasValue ? $" (через {hoursUntil.Value} часов)" : "";

        Log.Information($"  сегодня {timeStr} - {e.Subject}{hoursStr}");
        if (!string.IsNullOrEmpty(e.Description))
            Log.Information($"    📝 {e.Description}");
    }

    private void PrintTomorrowEvent(EventData e)
    {
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";

        Log.Information($"  завтра {timeStr} - {e.Subject}");
        if (!string.IsNullOrEmpty(e.Description))
            Log.Information($"    📝 {e.Description}");
    }

    private void PrintNextWeekEvent(EventData e)
    {
        var dayName = e.Date.ToString("dddd", new System.Globalization.CultureInfo("ru-RU"));
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";

        Log.Information($"  на след неделе ({dayName}) {timeStr} - {e.Subject}");
        if (!string.IsNullOrEmpty(e.Description))
            Log.Information($"    📝 {e.Description}");
    }

    private void PrintFutureEvent(EventData e)
    {
        var dateStr = e.Date.ToString("dd.MM.yyyy");
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";

        Log.Information($"  {dateStr} {timeStr} - {e.Subject}");
        if (!string.IsNullOrEmpty(e.Description))
            Log.Information($"    📝 {e.Description}");
    }

    private int? GetHoursUntil(EventData e)
    {
        if (!e.Time.HasValue)
            return null;

        var eventDateTime = e.Date.ToDateTime(e.Time.Value);
        var now = _dateTimeProvider.Now;
        var hours = (eventDateTime - now).TotalHours;

        return hours > 0 ? (int)Math.Ceiling(hours) : null;
    }
}