using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Senders;

public interface IReminderScheduler
{
    Task InitializeAsync();
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
}