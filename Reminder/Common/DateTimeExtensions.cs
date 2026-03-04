namespace Reminder.Tests;

public static class DateTimeExtensions
{
    public static DateTime RoundToStartOfMinute(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
    }
}
