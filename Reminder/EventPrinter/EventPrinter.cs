using ReminderApp.Common;

namespace ReminderApp.EventPrinter;
public class EventPrinter : IEventPrinter
{
    private readonly HashSet<string> _seenEvents = [];

    public void PrintEvents(List<EventData> events)
    {
        foreach (var eventData in events)
        {
            var eventKey = GetEventKey(eventData);

            if (!_seenEvents.Contains(eventKey))
            {
                Console.WriteLine($"📅 Новое событие: {eventData.Time:dd.MM.yyyy HH:mm} {eventData.Subject}");
                if (eventData.Description != null)
                {
                    Console.WriteLine($"📝 {eventData.Description}");
                }

                _seenEvents.Add(eventKey);
            }
        }
    }

    private string GetEventKey(EventData eventData)
    {
        return $"{eventData.Time:yyyyMMddHHmmss}_{eventData.Subject}_{eventData.Description}";
    }
}
