using ReminderApp.Common;
using Xunit;

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

    public static void AssertEquals(this EventData actual, EventData expected)
    {
        Assert.Equal(expected.Date, actual.Date);
        Assert.Equal(expected.Time, actual.Time);
        Assert.Equal(expected.Subject, actual.Subject);
        Assert.Equal(expected.Description, actual.Description);
    }

    public static void AssertEquals(this EventData actual, List<EventData> expectedList)
    {
        Assert.Single(expectedList);
        actual.AssertEquals(expectedList[0]);
    }
}
