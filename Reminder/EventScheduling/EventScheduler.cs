using ReminderApp.Common;

namespace ReminderApp.EventScheduling;

public class EventScheduler : IEventScheduler
{
    public List<EventData> GetDueEvents(List<EventData> events, Dictionary<string, DateTime> processed, DateTime now)
    {
        var dueEvents = new List<EventData>();

        foreach (var eventData in events)
        {
            var key = eventData.GetKey();

            if (processed.ContainsKey(key))
                continue;

            if (!IsEventDue(eventData, now))
                continue;

            dueEvents.Add(eventData);
        }

        return dueEvents;
    }
    private bool IsEventDue(EventData eventData, DateTime now)
    {
        if (eventData.Date.HasValue)
        {
            var eventDateTime = eventData.Date.Value.ToDateTime(eventData.Time ?? TimeOnly.MinValue);
            return eventDateTime <= now;
        }

        if (eventData.Time.HasValue)
        {
            var todayEventTime = DateTime.Today.Add(eventData.Time.Value.ToTimeSpan());
            return todayEventTime <= now;
        }

        return false;
    }
}
