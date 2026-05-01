namespace ReminderApp.Common;

public class EventData
{
    public required DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    
    public string GetKey()
    {
        // Human-readable key: date_time_name_checksum
        var datePart = Date.ToString("yyyy-MM-dd");
        var timePart = Time?.ToString("HHmm") ?? "----";
        var namePart = (Subject ?? Description ?? "").Replace(" ", "_").ToLowerInvariant();
        var checkPart = (datePart + timePart + namePart).GetHashCode().ToString("x8");
        
        return $"{datePart}_{timePart}_{namePart}_{checkPart}";
    }
}
