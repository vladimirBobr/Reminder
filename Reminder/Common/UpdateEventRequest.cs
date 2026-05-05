using System.Text.Json.Serialization;

namespace ReminderApp.Common;

public class UpdateEventRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    
    public TimeOnly? GetTimeAsTimeOnly()
    {
        if (string.IsNullOrEmpty(Time)) return null;
        if (TimeOnly.TryParse(Time, out var result)) return result;
        return null;
    }
    
    public DateOnly? GetDateAsDateOnly()
    {
        if (string.IsNullOrEmpty(Date)) return null;
        if (DateOnly.TryParse(Date, out var result)) return result;
        return null;
    }
}