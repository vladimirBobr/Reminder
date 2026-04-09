using ReminderApp.Common;

namespace ReminderApp.EventReading.LocalFile;

public interface IEventReader
{
    Task<List<EventData>> ReadEventsAsync();
}
