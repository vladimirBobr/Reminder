using ReminderApp.Common;

namespace ReminderApp.EventReading;

public interface IEventReader
{
    Task<List<EventData>> ReadEventsAsync();
}
