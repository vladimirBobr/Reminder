using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Senders;

public interface IDigestSender
{
    Task InitializeAsync();
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
}