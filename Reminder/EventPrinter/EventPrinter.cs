using ReminderApp.Common;

namespace ReminderApp.EventPrinter;
public class EventPrinter : IEventPrinter
{
    private readonly HashSet<string> _seenEvents = [];

    public void PrintEvents(List<EventData> events)
    {
        foreach (var eventData in events)
        {
            var eventKey = eventData.GetKey();

            if (!_seenEvents.Contains(eventKey))
            {
                var timeStr = eventData.Date.HasValue
                    ? eventData.Date.Value.ToDateTime(eventData.Time ?? TimeOnly.MinValue).ToString("dd.MM.yyyy HH:mm")
                    : eventData.Time.HasValue
                        ? eventData.Time.Value.ToString("HH:mm")
                        : "без даты и времени";

                Console.WriteLine($"📅 Новое событие: {timeStr} {eventData.Subject}");
                if (eventData.Description != null)
                {
                    Console.WriteLine($"📝 {eventData.Description}");
                }

                _seenEvents.Add(eventKey);
            }
        }
    }
}
