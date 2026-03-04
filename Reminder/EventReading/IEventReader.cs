using ReminderApp.Events;

namespace ReminderApp.EventReading;

public interface IEventReader
{
    Task<List<EventData>> ReadEventsAsync();
}
