using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Senders;

public interface IReminderSender
{
    Task InitializeAsync();
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
}