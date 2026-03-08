using ReminderApp.Common;
using ReminderApp.EventNotification;

namespace Reminder.Tests.EventProcessing.Helpers;

public class TestNotifier : INotifier
{
    public EventData? LastNotifiedEvent { get; private set; }
    public List<EventData> NotifiedEvents { get; } = new();

    public void Notify(EventData eventData)
    {
        LastNotifiedEvent = eventData;
        NotifiedEvents.Add(eventData);
    }
}
