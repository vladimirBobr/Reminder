using System.Text.Json.Serialization;

namespace ReminderApp.Common;

public class AddEventRequest
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }
    
    [JsonPropertyName("time")]
    public string? Time { get; set; }
    
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    public DateOnly? GetDateAsDateOnly()
    {
        if (string.IsNullOrEmpty(Date))
            return DateOnly.FromDateTime(DateTime.Today);
            
        if (DateOnly.TryParse(Date, out var date))
            return date;
            
        return null;
    }
    
    public TimeOnly? GetTimeAsTimeOnly()
    {
        if (string.IsNullOrEmpty(Time))
            return null;
            
        if (TimeOnly.TryParse(Time, out var time))
            return time;
            
        return null;
    }
}