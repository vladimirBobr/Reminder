namespace ReminderApp.Common;

public class EventData
{
    public DateTime Time { get; set; }
    public string? Subject { get; set; }
    public string Description { get; set; } = "";
}
