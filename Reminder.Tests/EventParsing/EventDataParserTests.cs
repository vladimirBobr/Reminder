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

        Expect.Event("Встреча с клиентом")
            .WithDate(10, 4, 2026)
            .WithTime(18, 30)
            .AssertMatches(events);
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

        Expect.Event("Встреча с клиентом")
            .WithDate(10, 4, 2026)
            .WithTime(18, 30)
            .WithDescription("Обсудить проект")
            .AssertMatches(events);
    }

    [Fact]
    public void ParseEvents_WithoutTime_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Просто событие без времени
            """;

        var events = _parser.ParseEvents(content);

        Expect.Event("Просто событие без времени")
            .WithDate(10, 4, 2026)
            .AssertMatches(events);
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

        Expect.Event("Важное событие")
            .WithDate(10, 4, 2026)
            .WithDescription("Это описание события")
            .AssertMatches(events);
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
        
        Expect.Event("Утренняя планёрка")
            .WithTime(9, 0)
            .AssertMatches(events[0]);
        
        Expect.Event("Совещание")
            .WithTime(14, 30)
            .AssertMatches(events[1]);
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
        
        Expect.Event("Событие на пятницу")
            .WithDate(10, 4, 2026)
            .WithTime(10, 0)
            .AssertMatches(events[0]);
        
        Expect.Event("Событие на субботу")
            .WithDate(11, 4, 2026)
            .WithTime(11, 0)
            .AssertMatches(events[1]);
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

        Expect.Event(null)
            .WithTime(18, 30)
            .AssertMatches(events);
    }

    [Fact]
    public void ParseEvents_TimeWithSingleDigitHour_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            9:30 Утреннее событие
            """;

        var events = _parser.ParseEvents(content);

        Expect.Event("Утреннее событие")
            .WithTime(9, 30)
            .AssertMatches(events);
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

        Expect.Event("Встреча")
            .WithTime(18, 30)
            .WithDescription("Строка 1 описания" + Environment.NewLine + "Строка 2 описания")
            .AssertMatches(events);
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