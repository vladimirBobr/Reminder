using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public partial class EventDataParserTests
{
    private readonly FileParser _parser = new();

    [Fact]
    public void ParseEvents_WithTimeAtStart_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            18:30 Встреча с клиентом
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        var evt = events[0];
        Assert.Equal(new DateOnly(2026, 4, 10), evt.Date);
        Assert.Equal(new TimeOnly(18, 30), evt.Time);
        Assert.Equal("Встреча с клиентом", evt.Subject);
    }

    [Fact]
    public void ParseEvents_WithTimeAndDescription_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            18:30 Встреча с клиентом
            Обсудить проект
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        var evt = events[0];
        Assert.Equal(new DateOnly(2026, 4, 10), evt.Date);
        Assert.Equal(new TimeOnly(18, 30), evt.Time);
        Assert.Equal("Встреча с клиентом", evt.Subject);
        Assert.Equal("Обсудить проект", evt.Description);
    }

    [Fact]
    public void ParseEvents_WithoutTime_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Просто событие без времени
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        var evt = events[0];
        Assert.Equal(new DateOnly(2026, 4, 10), evt.Date);
        Assert.Null(evt.Time);
        Assert.Equal("Просто событие без времени", evt.Subject);
    }

    [Fact]
    public void ParseEvents_WithoutTimeWithDescription_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Важное событие
            Это описание события
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        var evt = events[0];
        Assert.Equal(new DateOnly(2026, 4, 10), evt.Date);
        Assert.Null(evt.Time);
        Assert.Equal("Важное событие", evt.Subject);
        Assert.Equal("Это описание события", evt.Description);
    }

    [Fact]
    public void ParseEvents_WithMultipleEvents_ParsesAll()
    {
        var content = """
            # 10.04.2026 #
            09:00 Утренняя планёрка

            14:30 Совещание
            """;

        var events = _parser.ParseEvents(content);

        Assert.Equal(2, events.Count);
        
        Assert.Equal(new TimeOnly(9, 0), events[0].Time);
        Assert.Equal("Утренняя планёрка", events[0].Subject);
        
        Assert.Equal(new TimeOnly(14, 30), events[1].Time);
        Assert.Equal("Совещание", events[1].Subject);
    }

    [Fact]
    public void ParseEvents_WithMultipleDateSections_ParsesAll()
    {
        var content = """
            # 10.04.2026 #
            10:00 Событие на пятницу

            # 11.04.2026 #
            11:00 Событие на субботу
            """;

        var events = _parser.ParseEvents(content);

        Assert.Equal(2, events.Count);
        
        Assert.Equal(new DateOnly(2026, 4, 10), events[0].Date);
        Assert.Equal(new TimeOnly(10, 0), events[0].Time);
        
        Assert.Equal(new DateOnly(2026, 4, 11), events[1].Date);
        Assert.Equal(new TimeOnly(11, 0), events[1].Time);
    }

    [Fact]
    public void ParseEvents_WithEmptyBlock_SkipsEmpty()
    {
        var content = """
            # 10.04.2026 #
            10:00 Событие

            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
    }

    [Fact]
    public void ParseEvents_WithSingleLineTimeOnly_ParsesSubject()
    {
        var content = """
            # 10.04.2026 #
            18:30
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        var evt = events[0];
        Assert.Equal(new TimeOnly(18, 30), evt.Time);
        Assert.Null(evt.Subject);
    }

    [Fact]
    public void ParseEvents_TimeWithSingleDigitHour_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            9:30 Утреннее событие
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        Assert.Equal(new TimeOnly(9, 30), events[0].Time);
    }

    [Fact]
    public void ParseEvents_TimeWithMultilineDescription_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            18:30 Встреча
            Строка 1 описания
            Строка 2 описания
            """;

        var events = _parser.ParseEvents(content);

        Assert.Single(events);
        var evt = events[0];
        Assert.Equal("Встреча", evt.Subject);
        Assert.Equal("Строка 1 описания" + Environment.NewLine + "Строка 2 описания", evt.Description);
    }

    [Fact]
    public void ParseEvents_EmptyContent_ReturnsEmptyList()
    {
        var events = _parser.ParseEvents("");

        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_NoEvents_ReturnsEmptyList()
    {
        var content = """
            # 10.04.2026 #
            """;

        var events = _parser.ParseEvents(content);

        Assert.Empty(events);
    }
}