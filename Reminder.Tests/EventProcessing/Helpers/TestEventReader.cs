using ReminderApp.EventReading;
using ReminderApp.Events;

namespace Reminder.Tests.EventProcessing.Helpers;

public class TestEventReader : IEventReader
{
    private List<EventData> _events = new();

    public void SetEvents(List<EventData> events)
    {
        _events = events;
    }

    public Task<List<EventData>> ReadEventsAsync()
    {
        return Task.FromResult(_events);
    }
}
