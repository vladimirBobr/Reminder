using ReminderApp.Common;

namespace ReminderApp.EventPrinter;

public interface IEventPrinter
{
    void PrintEvents(List<EventData> events);
}
