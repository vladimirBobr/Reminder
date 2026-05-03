namespace ReminderApp.Common;

public class EventData
{
    public required DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    
    public string GetKey()
    {
        // Format: {date}_{time}_{4hash} or {date}_{4hash} if no time
        var datePart = Date.ToString("yyyy-MM-dd");
        
        // Create hash from content
        var content = (Subject ?? "") + "|" + (Description ?? "");
        var hashBytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        var hash4 = Convert.ToHexString(hashBytes)[..4].ToLowerInvariant();
        
        if (Time.HasValue)
        {
            var timePart = Time.Value.ToString("HHmm");
            return $"{datePart}_{timePart}_{hash4}";
        }
        return $"{datePart}_{hash4}";
    }
}
