using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public partial class EventDataParserTests
{
    private readonly FileParser _parser = new();

    // =============== Базовые сценарии ===============

    [Fact]
    public void ParseEvents_WhenEmptyString_ReturnsEmptyList()
    {
        var events = _parser.ParseEvents("");
        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_WhenWhitespaceOnly_ReturnsEmptyList()
    {
        var events = _parser.ParseEvents("   \n\t\r   ");
        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_WhenNoHeaders_ReturnsEmptyList()
    {
        var content = "просто текст без заголовков\nещё текст";
        var events = _parser.ParseEvents(content);
        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_WhenSingleDateSection_ReturnsEvents()
    {
        var content = """
            # 10.04.2026 #

            событие 1

            событие 2
            """;
        var events = _parser.ParseEvents(content);

        new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 10), Subject = "событие 1" },
            new() { Date = new DateOnly(2026, 4, 10), Subject = "событие 2" }
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WhenMultipleDateSections_ReturnsAllEvents()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # 11.04.2026 #
            событие на субботу

            # 12.04.2026 #
            событие на воскресенье
            """;
        var events = _parser.ParseEvents(content);

        new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 10), Subject = "событие на пятницу" },
            new() { Date = new DateOnly(2026, 4, 11), Subject = "событие на субботу" },
            new() { Date = new DateOnly(2026, 4, 12), Subject = "событие на воскресенье" }
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithDifferentDatesSection_ParsesEvents()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # different_dates_section #

            02.04.2026 Чт Дежурный ФТ

            10.04.2026 Пт Дежурный ФТ
            """;
        var events = _parser.ParseEvents(content);

        new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 10), Subject = "событие на пятницу" },
            new() { Date = new DateOnly(2026, 4, 2), Subject = "Чт Дежурный ФТ" },
            new() { Date = new DateOnly(2026, 4, 10), Subject = "Пт Дежурный ФТ" }
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WhenEventBlocksSeparatedByEmptyLines_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #

            первое событие
            многострочное


            второе событие



            третье событие
            """;
        var events = _parser.ParseEvents(content);

        Assert.Equal(3, events.Count);
        Assert.Equal("первое событие", events[0].Subject);
        Assert.Equal("второе событие", events[1].Subject);
        Assert.Equal("третье событие", events[2].Subject);
    }

    [Fact]
    public void ParseEvents_WhenSectionWithoutEvents_ReturnsEmptyList()
    {
        var content = """
            # 10.04.2026 #

            # 11.04.2026 #
            событие есть
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 11), Subject = "событие есть" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithAllSections_ParsesDateAndDifferentDates()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # different_dates_section #

            02.04.2026 Чт Дежурный ФТ
            """;
        var events = _parser.ParseEvents(content);

        new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 10), Subject = "событие на пятницу" },
            new() { Date = new DateOnly(2026, 4, 2), Subject = "Чт Дежурный ФТ" }
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithVariousHeaderFormats_ParsesCorrectly()
    {
        var content = """
            # 12.04.2026 #
            событие

            # different_dates_section #
            15.04.2026 другое событие
            """;
        var events = _parser.ParseEvents(content);

        new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 12), Subject = "событие" },
            new() { Date = new DateOnly(2026, 4, 15), Subject = "другое событие" }
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithManyEquals_ParsesCorrectly()
    {
        var content = """
            ====================# 12.04.2026 #=====================
            событие
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 12), Subject = "событие" }.AssertEquals(events);
    }

    // =============== Краевые сценарии ===============

    [Fact]
    public void ParseEvents_WithDateWithoutClosingHash_ReturnsEmpty()
    {
        var content = """
            # 10.04.2026
            событие
            """;
        var events = _parser.ParseEvents(content);
        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_WithLeadingZerosInDate_ParsesCorrectly()
    {
        var content = """
            # 01.01.2026 #
            новогоднее событие
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 1, 1), Subject = "новогоднее событие" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithEmptyLinesAfterHeader_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #



            событие после пустых строк
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = "событие после пустых строк" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithSpecialCharacters_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Событие с <тегами> и "кавычками"
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = "Событие с <тегами> и \"кавычками\"" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithRussianText_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            событие с русским текстом
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = "событие с русским текстом" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithOnlyHashSigns_NoEvents()
    {
        var content = """
            #
            текст без даты
            """;
        var events = _parser.ParseEvents(content);
        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_WithSectionAfterDifferentDates_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            событие на дату

            # different_dates_section #
            05.04.2026 другое событие
            """;
        var events = _parser.ParseEvents(content);

        new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 10), Subject = "событие на дату" },
            new() { Date = new DateOnly(2026, 4, 5), Subject = "другое событие" }
        }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithNotesFirstThenDate_ParsesCorrectly()
    {
        var content = """
            # notes_section #
            заметка

            # 10.04.2026 #
            событие
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = "событие" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithVeryLongLine_ParsesCorrectly()
    {
        var veryLongText = new string('а', 10000);
        var content = $"""
            # 10.04.2026 #
            {veryLongText}
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = veryLongText }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithMultipleEmptySections_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #

            # 11.04.2026 #

            # 12.04.2026 #

            """;
        var events = _parser.ParseEvents(content);
        Assert.Empty(events);
    }

    [Fact]
    public void ParseEvents_DifferentDatesSectionIsCaseInsensitive()
    {
        var content = """
            # DIFFERENT_DATES_SECTION #
            02.04.2026 событие
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 2), Subject = "событие" }.AssertEquals(events);
    }

    // =============== Детали парсинга EventData ===============

    [Fact]
    public void ParseEvents_WithTimeAtStart_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            18:30 Встреча с клиентом
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Time = new TimeOnly(18, 30), Subject = "Встреча с клиентом" }.AssertEquals(events);
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

        new EventData { Date = new DateOnly(2026, 4, 10), Time = new TimeOnly(18, 30), Subject = "Встреча с клиентом", Description = "Обсудить проект" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithoutTime_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Просто событие без времени
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = "Просто событие без времени" }.AssertEquals(events);
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

        new EventData { Date = new DateOnly(2026, 4, 10), Subject = "Важное событие", Description = "Это описание события" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_TimeWithSingleDigitHour_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            9:30 Утреннее событие
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Time = new TimeOnly(9, 30), Subject = "Утреннее событие" }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithSingleLineTimeOnly_ParsesSubject()
    {
        var content = """
            # 10.04.2026 #
            18:30
            """;
        var events = _parser.ParseEvents(content);

        new EventData { Date = new DateOnly(2026, 4, 10), Time = new TimeOnly(18, 30), Subject = null }.AssertEquals(events);
    }

    [Fact]
    public void ParseEvents_WithTimeWithMultilineDescription_ParsesCorrectly()
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
            Description = "Строка 1 описания" + Environment.NewLine + "Строка 2 описания"
        }.AssertEquals(events);
    }
}