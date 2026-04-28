namespace ReminderApp.EventReading;

public interface IEventReader
{
    Task<ParsedFileData> ReadEventsAsync();
}