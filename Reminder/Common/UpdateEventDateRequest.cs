namespace ReminderApp.Common;

public class UpdateEventDateRequest
{
    public required string Key { get; set; }
    public required string NewDate { get; set; }
}