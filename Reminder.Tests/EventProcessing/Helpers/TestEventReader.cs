using ReminderApp.Common;
using ReminderApp.EventParsing;
using ReminderApp.EventReading;

namespace Reminder.Tests.EventProcessing.Helpers;

public class TestEventReader : IEventReader
{
    private List<EventData> _events = new();
    private List<ShoppingItem> _shoppingItems = new();

    public void SetEvents(List<EventData> events)
    {
        _events = events;
    }

    public void SetShoppingItems(List<ShoppingItem> items)
    {
        _shoppingItems = items;
    }

    public Task<ParsedFileData> ReadEventsAsync()
    {
        return Task.FromResult(new ParsedFileData { Events = _events, ShoppingItems = _shoppingItems });
    }
}