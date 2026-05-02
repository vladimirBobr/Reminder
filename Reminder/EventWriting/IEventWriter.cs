namespace ReminderApp.EventWriting;

public record EventWriteResult(bool Success, string? ErrorMessage = null);

public interface IEventWriter
{
    Task<EventWriteResult> UpdateEventDateAsync(string key, DateOnly newDate);
}