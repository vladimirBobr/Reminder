using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Senders;

public interface IDigestSender
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
    Task SendDailyDigestAsync(List<EventData> events, DateTime now);
}