using ReminderApp.Common;

namespace ReminderApp.EventScheduling;

public class EventScheduler : IEventScheduler
{
    public List<EventData> GetDueEvents(List<EventData> events, Dictionary<string, DateTime> processed, DateTime now)
    {
        var dueEvents = new List<EventData>();

        foreach (var eventData in events)
        {
            var timeLeft = (eventData.Time - now).TotalSeconds;
            var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";

            if (timeLeft <= 0 && !processed.ContainsKey(notifyKey))
            {
                dueEvents.Add(eventData);
            }
        }

        return dueEvents;
    }
}
