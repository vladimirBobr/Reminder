using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public static class Helpers
{
    public static void AssertEventData(
        EventData actual,
        DateTime? expectedTime = null,
        string? expectedSubject = null,
        string? expectedDescription = null)
    {
        if (expectedTime.HasValue)
            Assert.Equal(expectedTime.Value, actual.Time);
        Assert.Equal(expectedSubject, actual.Subject);
        Assert.Equal(expectedDescription, actual.Description);
    }

    public static void AssertErrorEventData(
        EventData actual,
        string originalBlock)
    {
        Assert.Equal(DateTime.Today, actual.Time);
        Assert.Equal(Parser.ErrorSubject, actual.Subject);
        Assert.Equal(originalBlock, actual.Description);
    }
}
