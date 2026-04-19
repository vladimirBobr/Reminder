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

        // Собираем одно сообщение
        var sb = new System.Text.StringBuilder();

        if (todayEvents.Count > 0)
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📅 СЕГОДНЯ");
            sb.AppendLine("═══════════════════════════════════════");
            foreach (var e in todayEvents.OrderBy(x => x.Time))
                sb.AppendLine(FormatTodayEvent(e));
        }

        if (tomorrowEvents.Count > 0)
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📅 ЗАВТРА");
            sb.AppendLine("═══════════════════════════════════════");
            foreach (var e in tomorrowEvents.OrderBy(x => x.Time))
                sb.AppendLine(FormatTomorrowEvent(e));
        }

        if (nextWeekEvents.Count > 0)
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📅 НА СЛЕДУЮЩЕЙ НЕДЕЛЕ");
            sb.AppendLine("═══════════════════════════════════════");
            foreach (var e in nextWeekEvents.OrderBy(x => x.Date).ThenBy(x => x.Time))
                sb.AppendLine(FormatNextWeekEvent(e));
        }

        if (futureEvents.Count > 0)
        {
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("📅 В БУДУЩЕМ");
            sb.AppendLine("═══════════════════════════════════════");
            foreach (var e in futureEvents.OrderBy(x => x.Date).ThenBy(x => x.Time))
                sb.AppendLine(FormatFutureEvent(e));
        }

        if (sb.Length > 0)
            Log.Information("{Events}", sb.ToString().TrimEnd());
    }

    private string FormatTodayEvent(EventData e)
    {
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";
        var hoursUntil = GetHoursUntil(e);
        var hoursStr = hoursUntil.HasValue ? $" (через {hoursUntil.Value} часов)" : "";

        var desc = string.IsNullOrEmpty(e.Description) ? "" : $"\n   📝 {e.Description}";
        return $"  сегодня {timeStr} - {e.Subject}{hoursStr}{desc}";
    }

    private string FormatTomorrowEvent(EventData e)
    {
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";
        var desc = string.IsNullOrEmpty(e.Description) ? "" : $"\n   📝 {e.Description}";
        return $"  завтра {timeStr} - {e.Subject}{desc}";
    }

    private string FormatNextWeekEvent(EventData e)
    {
        var dayName = e.Date.ToString("dddd", new System.Globalization.CultureInfo("ru-RU"));
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";
        var desc = string.IsNullOrEmpty(e.Description) ? "" : $"\n   📝 {e.Description}";
        return $"  на след неделе ({dayName}) {timeStr} - {e.Subject}{desc}";
    }

    private string FormatFutureEvent(EventData e)
    {
        var dateStr = e.Date.ToString("dd.MM.yyyy");
        var timeStr = e.Time?.ToString("HH:mm") ?? "---";
        var desc = string.IsNullOrEmpty(e.Description) ? "" : $"\n   📝 {e.Description}";
        return $"  {dateStr} {timeStr} - {e.Subject}{desc}";
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