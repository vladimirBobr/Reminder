using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Processors;

public interface IDailyDigestProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
    Task SendDailyDigestAsync(List<EventData> events, DateTime now);
}