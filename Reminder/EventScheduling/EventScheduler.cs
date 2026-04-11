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
        var eventDateTime = eventData.Date.ToDateTime(eventData.Time ?? TimeOnly.MinValue);
        return eventDateTime <= now;
    }
}
