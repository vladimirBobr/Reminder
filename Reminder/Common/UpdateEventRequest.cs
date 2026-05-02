namespace ReminderApp.Common;

public class UpdateEventRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Description { get; set; }
}