using ReminderApp.Common;

namespace Reminder.Tests.EventParsing;

/// <summary>
/// Билдер для создания ожидаемого события EventData.
/// Пример: Expect.Event("Встреча").WithTime(18, 30).WithDate(10, 4, 2026)
/// </summary>
public class EventDataBuilder
{
    public DateOnly? Date { get; private set; }
    public TimeOnly? Time { get; private set; }
    public string? Subject { get; set; }
    public string? Description { get; private set; }

    public EventDataBuilder WithDate(int day, int month, int year)
    {
        Date = new DateOnly(year, month, day);
        return this;
    }

    public EventDataBuilder WithTime(int hour, int minute)
    {
        Time = new TimeOnly(hour, minute);
        return this;
    }

    public EventDataBuilder WithSubject(string subject)
    {
        Subject = subject;
        return this;
    }

    public EventDataBuilder WithDescription(string description)
    {
        Description = description;
        return this;
    }

    public void AssertMatches(EventData actual)
    {
        if (Date.HasValue)
            Assert.Equal(Date.Value, actual.Date);
        if (Time.HasValue)
            Assert.Equal(Time.Value, actual.Time);
        
        Assert.Equal(Subject, actual.Subject);
        Assert.Equal(Description, actual.Description);
    }

    public void AssertMatches(List<EventData> actualList)
    {
        Assert.Single(actualList);
        AssertMatches(actualList[0]);
    }
}