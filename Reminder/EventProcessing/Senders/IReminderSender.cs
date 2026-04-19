using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Senders;

public interface IReminderSender
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
}