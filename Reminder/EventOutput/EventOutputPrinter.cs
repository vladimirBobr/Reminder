using ReminderApp.Common;

namespace ReminderApp.EventOutput;
public class EventOutputPrinter : IEventOutputPrinter
{
    private readonly HashSet<string> _seenEvents = [];

    public void PrintEvents(List<EventData> events)
    {
        foreach (var eventData in events)
        {
            var eventKey = eventData.GetKey();

            if (!_seenEvents.Contains(eventKey))
            {
                var timeStr = eventData.Date.ToDateTime(eventData.Time ?? TimeOnly.MinValue).ToString("dd.MM.yyyy HH:mm");

                Log.Information($"📅 Новое событие: {timeStr} {eventData.Subject}");
                if (eventData.Description != null)
                {
                    Log.Information($"📝 {eventData.Description}");
                }

                _seenEvents.Add(eventKey);
            }
        }
    }
}