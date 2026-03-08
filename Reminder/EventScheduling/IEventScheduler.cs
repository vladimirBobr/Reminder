using ReminderApp.Common;

namespace ReminderApp.EventScheduling;

public interface IEventScheduler
{
    List<EventData> GetDueEvents(List<EventData> events, Dictionary<string, DateTime> processed, DateTime now);
}
