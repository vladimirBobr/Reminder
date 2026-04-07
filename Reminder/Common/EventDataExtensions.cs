namespace ReminderApp.Common;

public static class EventDataExtensions
{
    public static string GetKey(this EventData eventData)
    {
        if (eventData.Date.HasValue)
        {
            var dateTime = eventData.Date.Value.ToDateTime(eventData.Time ?? TimeOnly.MinValue);
            return $"{dateTime:yyyyMMddHHmmss}-{eventData.Subject}";
        }

        if (eventData.Time.HasValue)
        {
            return $"{eventData.Time:HHmmss}-{eventData.Subject}";
        }

        return $"nodate-{eventData.Subject}";
    }
}
