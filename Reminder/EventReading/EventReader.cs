using ReminderApp.EventParsing;
using ReminderApp.Events;

namespace ReminderApp.EventReading;

public class EventReader : IEventReader
{
    private readonly Parser _parser = new();

    public async Task<List<EventData>> ReadEventsAsync()
    {
        var content = await File.ReadAllTextAsync("events.txt");
        return _parser.ParseEvents(content);
    }
}
