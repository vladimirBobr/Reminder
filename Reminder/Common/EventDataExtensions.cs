namespace ReminderApp.Common;

public static class EventDataExtensions
{
    public static string GetKey(this EventData eventData)
    {
        var dateTime = eventData.Date.ToDateTime(eventData.Time ?? TimeOnly.MinValue);
        return $"{dateTime:yyyyMMddHHmmss}-{eventData.Subject}";
    }
}
