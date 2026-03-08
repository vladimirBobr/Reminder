using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public class ParserOneEventTests
{
    private readonly Parser _parser = new();

    [Fact]
    public void ParseEventBlock_WhenValidDateAndTime_ReturnsCorrectEventData()
    {
        var block = """
                    24.03.2026 22:00 проверить автоплатежи
                    (не забыть про новый платёж тренеру)
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: "(не забыть про новый платёж тренеру)"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenValidDateOnly_ReturnsCorrectEventData()
    {
        var block = """
                    24.03.2026
                    Просто напоминание без времени
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 0, 0, 0),
            expectedSubject: null,
            expectedDescription: "Просто напоминание без времени"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenOnlyTime_ReturnsTodayDateAndCorrectEventData()
    {
        var block = """
                    22:00 проверить автоплатежи
                    (не забыть про новый платёж тренеру)
                    """;

        var eventData = _parser.ParseEventBlock(block);
        var today = DateTime.Today;

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(today.Year, today.Month, today.Day, 22, 0, 0),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: "(не забыть про новый платёж тренеру)"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenOnlyTimeAndNoSubject_ReturnsTodayDateAndCorrectEventData()
    {
        var block = """
                    22:00
                    Напоминание без subject в первой строке
                    """;

        var eventData = _parser.ParseEventBlock(block);
        var today = DateTime.Today;

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(today.Year, today.Month, today.Day, 22, 0, 0),
            expectedSubject: null,
            expectedDescription: "Напоминание без subject в первой строке"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenInvalidDate_ReturnsErrorEvent()
    {
        var block = """
                    32.03.2026 22:00 проверить автоплатежи
                    (не забыть про новый платёж тренеру)
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertErrorEventData(
            actual: eventData,
            originalBlock: block
        );
    }

    [Fact]
    public void ParseEventBlock_WhenInvalidTimeFirst_ReturnsErrorEvent()
    {
        var block = """
                    25:00 проверить автоплатежи
                    (не забыть про новый платёж тренеру)
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertErrorEventData(
            actual: eventData,
            originalBlock: block
        );
    }

    [Fact]
    public void ParseEventBlock_WhenSubjectOnly_ReturnsErrorEvent()
    {
        var block = """
                    просто текст без даты и времени
                    (не забыть про новый платёж тренеру)
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertErrorEventData(
            actual: eventData,
            originalBlock: block
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDescriptionHasMultipleLines_ReturnsCorrectDescription()
    {
        var block = """
                    24.03.2026 22:00 проверить автоплатежи
                    Первая строка описания
                    Вторая строка описания
                    Третья строка описания
                    """;

        var expectedDescription = "Первая строка описания" + Environment.NewLine +
                                  "Вторая строка описания" + Environment.NewLine +
                                  "Третья строка описания";

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: expectedDescription
        );
    }

    [Fact]
    public void ParseEventBlock_WhenTimeWithSeconds_ReturnsCorrectEventData()
    {
        var block = """
                    24.03.2026 22:00:30 проверить автоплатежи
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 30),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenOnlyTimeWithSeconds_ReturnsTodayDateAndCorrectEventData()
    {
        var block = """
                    22:00:30 проверить автоплатежи
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);
        var today = DateTime.Today;

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(today.Year, today.Month, today.Day, 22, 0, 30),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDateWithLeadingZeros_ReturnsCorrectDate()
    {
        var block = """
                    05.03.2026 проверить
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 5, 0, 0, 0),
            expectedSubject: "проверить",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenTimeWithLeadingZeros_ReturnsCorrectTime()
    {
        var block = """
                    05:05 проверить
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);
        var today = DateTime.Today;

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(today.Year, today.Month, today.Day, 5, 5, 0),
            expectedSubject: "проверить",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDateAndMultipleSpaces_ReturnsCorrectParsing()
    {
        var block = """
                    24.03.2026    22:00    проверить    автоплатежи
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenSubjectContainsNumbers_ReturnsFullSubject()
    {
        var block = """
                    22:00 позвонить по номеру 123-45-67
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);
        var today = DateTime.Today;

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(today.Year, today.Month, today.Day, 22, 0, 0),
            expectedSubject: "позвонить по номеру 123-45-67",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDateAndSubjectStartsWithTimeLikeText_ReturnsCorrectSubject()
    {
        var block = """
                    24.03.2026 встреча в 22:00
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 0, 0, 0),
            expectedSubject: "встреча в 22:00",
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDateMinValue_ReturnsCorrectDate()
    {
        var block = """
                    01.01.0001
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(1, 1, 1, 0, 0, 0),
            expectedSubject: null,
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDateMaxValue_ReturnsCorrectDate()
    {
        var block = """
                    31.12.9999
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(9999, 12, 31, 0, 0, 0),
            expectedSubject: null,
            expectedDescription: "Описание"
        );
    }

    [Fact]
    public void ParseEventBlock_WhenEmptyString_ThrowsException()
    {
        Assert.Throws<Exception>(() => _parser.ParseEventBlock(""));
    }

    [Fact]
    public void ParseEventBlock_WhenWhitespaceString_ThrowsException()
    {
        Assert.Throws<Exception>(() => _parser.ParseEventBlock("   "));
    }

    [Fact]
    public void ParseEventBlock_WhenNullString_ThrowsException()
    {
        Assert.Throws<Exception>(() => _parser.ParseEventBlock(null!));
    }

    [Fact]
    public void ParseEventBlock_WhenDateIsPartial_ReturnsErrorEvent()
    {
        var block = """
                    24.03.
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertErrorEventData(
            actual: eventData,
            originalBlock: block
        );
    }

    [Fact]
    public void ParseEventBlock_WhenDateIsInvalidFormat_ReturnsErrorEvent()
    {
        var block = """
                    2026-03-24
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertErrorEventData(
            actual: eventData,
            originalBlock: block
        );
    }

    [Fact]
    public void ParseEventBlock_WhenInvalidTime_ReturnsErrorEvent()
    {
        var block = """
                    24.03.2026 25:00 проверить
                    Описание
                    """;

        var eventData = _parser.ParseEventBlock(block);

        Helpers.AssertEventData(
            actual: eventData,
            expectedTime: new DateTime(2026, 3, 24, 0, 0, 0), // дата без времени
            expectedSubject: "25:00 проверить",
            expectedDescription: "Описание"
        );
    }
}
