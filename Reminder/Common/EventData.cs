namespace ReminderApp.Common;

public class EventData
{
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
}
