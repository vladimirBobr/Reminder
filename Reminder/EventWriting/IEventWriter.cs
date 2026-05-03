namespace ReminderApp.EventWriting;

public record EventWriteResult(bool Success, string? ErrorMessage = null, string? NewKey = null);

public interface IEventWriter
{
    Task<EventWriteResult> UpdateEventDateAsync(string key, DateOnly newDate);
    Task<EventWriteResult> UpdateEventAsync(string key, string? subject, string? description, TimeOnly? time = null);
    Task<EventWriteResult> DeleteEventAsync(string key);
}