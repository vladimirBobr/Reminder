using ReminderApp.Common;

namespace ReminderApp.EventOutput;

public interface IEventOutputPrinter
{
    void PrintEvents(List<EventData> events);
}