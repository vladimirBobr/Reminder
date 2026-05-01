namespace ReminderApp.Common;

public class UpdateEventsRequest
{
    public List<EventUpdateItem> Events { get; set; } = new();
}

public class EventUpdateItem
{
    public string Date { get; set; } = string.Empty;
    public string? Time { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
}