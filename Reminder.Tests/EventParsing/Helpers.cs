using ReminderApp.Common;

namespace Reminder.Tests.EventParsing;

public static class Helpers
{
    public static void AssertEventData(
        EventData actual,
        DateOnly? expectedDate = null,
        TimeOnly? expectedTime = null,
        string? expectedSubject = null,
        string? expectedDescription = null)
    {
        if (expectedDate.HasValue)
            Assert.Equal(expectedDate.Value, actual.Date);
        if (expectedTime.HasValue)
            Assert.Equal(expectedTime.Value, actual.Time);

        Assert.Equal(expectedSubject, actual.Subject);
        Assert.Equal(expectedDescription, actual.Description);
    }
}
