using ReminderApp.Common;

namespace ReminderApp.EventProcessing.Processors;

public interface IReminderProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
}