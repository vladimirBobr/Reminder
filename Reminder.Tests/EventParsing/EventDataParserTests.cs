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

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(18, 30),
            Subject = "Встреча с клиентом",
            Description = null,
        }.AssertEquals(events);
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

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(18, 30),
            Subject = "Встреча с клиентом",
            Description = "Обсудить проект",
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithoutTime_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Просто событие без времени
            """;

        var events = _parser.ParseEvents(content);

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = null,
            Subject = "Просто событие без времени",
            Description = null,
        }.AssertEquals(events);
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

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = null,
            Subject = "Важное событие",
            Description = "Это описание события",
        }.AssertEquals(events);
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
        
        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(9, 0),
            Subject = "Утренняя планёрка",
            Description = null,
        }.AssertEquals(events[0]);
        
        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(14, 30),
            Subject = "Совещание",
            Description = null,
        }.AssertEquals(events[1]);
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
        
        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(10, 0),
            Subject = "Событие на пятницу",
            Description = null,
        }.AssertEquals(events[0]);
        
        new EventData
        {
            Date = new DateOnly(2026, 4, 11),
            Time = new TimeOnly(11, 0),
            Subject = "Событие на субботу",
            Description = null,
        }.AssertEquals(events[1]);
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

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(18, 30),
            Subject = null,
            Description = null,
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_TimeWithSingleDigitHour_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            9:30 Утреннее событие
            """;

        var events = _parser.ParseEvents(content);

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(9, 30),
            Subject = "Утреннее событие",
            Description = null,
        }.AssertEquals(events);
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

        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(18, 30),
            Subject = "Встреча",
            Description = "Строка 1 описания" + Environment.NewLine + "Строка 2 описания",
        }.AssertEquals(events);
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

    [Fact]
    public void ParseEvents_WithDifferentDatesSection_ParsesCorrectly()
    {
        var content = """
            # different_dates_section #
            02.04.2026 Чт Дежурный ФТ
            """;

        var events = _parser.ParseEvents(content);

        new EventData
        {
            Date = new DateOnly(2026, 4, 2),
            Time = null,
            Subject = "Чт Дежурный ФТ",
            Description = null,
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithDifferentDatesSectionAndTime_ParsesCorrectly()
    {
        var content = """
            # different_dates_section #
            02.04.2026 14:00 Встреча
            """;

        var events = _parser.ParseEvents(content);

        new EventData
        {
            Date = new DateOnly(2026, 4, 2),
            Time = new TimeOnly(14, 0),
            Subject = "Встреча",
            Description = null,
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithNotesSection_ParsesCorrectly()
    {
        var content = """
            # notes_section #
            Моя заметка
            """;

        var events = _parser.ParseEvents(content);

        new EventData
        {
            Date = null,
            Time = null,
            Subject = "Моя заметка",
            Description = null,
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithNotesSectionAndTime_ParsesCorrectly()
    {
        var content = """
            # notes_section #
            15:00 Напоминание
            """;

        var events = _parser.ParseEvents(content);

        new EventData
        {
            Date = null,
            Time = new TimeOnly(15, 0),
            Subject = "Напоминание",
            Description = null,
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithAllSections_ParsesAll()
    {
        var content = """
            # 10.04.2026 #
            10:00 Событие на дату

            # different_dates_section #
            05.04.2026 Событие с датой

            # notes_section #
            Заметка
            """;

        var events = _parser.ParseEvents(content);

        Assert.Equal(3, events.Count);
        
        // DateSection
        new EventData
        {
            Date = new DateOnly(2026, 4, 10),
            Time = new TimeOnly(10, 0),
            Subject = "Событие на дату",
            Description = null,
        }.AssertEquals(events[0]);
        
        // DifferentDatesSection
        new EventData
        {
            Date = new DateOnly(2026, 4, 5),
            Time = null,
            Subject = "Событие с датой",
            Description = null,
        }.AssertEquals(events[1]);
        
        // NotesSection
        new EventData
        {
            Date = null,
            Time = null,
            Subject = "Заметка",
            Description = null,
        }.AssertEquals(events[2]);
    }
}