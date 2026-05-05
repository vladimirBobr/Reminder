namespace ReminderApp.EventWriting;

public record EventWriteResult(bool Success, string? ErrorMessage = null, string? NewKey = null);

public interface IEventWriter
{
    Task<EventWriteResult> UpdateEventAsync(string key, DateOnly? date, string? subject, string? description, TimeOnly? time = null);
    Task<EventWriteResult> DeleteEventAsync(string key);
}