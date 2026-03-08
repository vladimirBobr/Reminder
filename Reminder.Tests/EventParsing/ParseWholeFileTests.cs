using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public class ParserFileTests
{
    private readonly Parser _parser = new();

    [Fact]
    public void ParseEvents_WhenEmptyString_ReturnsEmptyList()
    {
        var result = _parser.ParseEvents("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseEvents_WhenWhitespaceString_ReturnsEmptyList()
    {
        var result = _parser.ParseEvents("   \n\t\r");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseEvents_WhenSingleEvent_ReturnsOneEventData()
    {
        var content = """
                      24.03.2026 22:00 проверить автоплатежи
                      Описание события
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Single(result);
        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "проверить автоплатежи",
            expectedDescription: "Описание события"
        );
    }

    [Fact]
    public void ParseEvents_WhenMultipleEventsSeparatedByOneEmptyLine_ReturnsMultipleEventData()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1

                      25.03.2026 10:00 событие 2
                      Описание 2
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Equal(2, result.Count);

        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );

        Helpers.AssertEventData(
            actual: result[1],
            expectedTime: new DateTime(2026, 3, 25, 10, 0, 0),
            expectedSubject: "событие 2",
            expectedDescription: "Описание 2"
        );
    }

    [Fact]
    public void ParseEvents_WhenMultipleEventsSeparatedByMultipleEmptyLines_ReturnsMultipleEventData()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1


                      25.03.2026 10:00 событие 2
                      Описание 2


                      26.03.2026 15:00 событие 3
                      Описание 3
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Equal(3, result.Count);

        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );

        Helpers.AssertEventData(
            actual: result[1],
            expectedTime: new DateTime(2026, 3, 25, 10, 0, 0),
            expectedSubject: "событие 2",
            expectedDescription: "Описание 2"
        );

        Helpers.AssertEventData(
            actual: result[2],
            expectedTime: new DateTime(2026, 3, 26, 15, 0, 0),
            expectedSubject: "событие 3",
            expectedDescription: "Описание 3"
        );
    }

    [Fact]
    public void ParseEvents_WhenActualLineSeparatorPresent_IgnoresEverythingBelow()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1

                      actual line:===============================================================

                      25.03.2026 10:00 событие 2 (не должно парситься)
                      Описание 2
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Single(result);
        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );
    }

    [Fact]
    public void ParseEvents_WhenActualLineSeparatorAtStart_ReturnsEmptyList()
    {
        var content = """
                      actual line:===============================================================
                      24.03.2026 22:00 событие (не должно парситься)
                      Описание
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseEvents_WhenActualLineSeparatorInMiddle_ReturnsOnlyAbove()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1

                      25.03.2026 10:00 событие 2
                      Описание 2

                      actual line:===============================================================

                      26.03.2026 15:00 событие 3 (не должно парситься)
                      Описание 3
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Equal(2, result.Count);

        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );

        Helpers.AssertEventData(
            actual: result[1],
            expectedTime: new DateTime(2026, 3, 25, 10, 0, 0),
            expectedSubject: "событие 2",
            expectedDescription: "Описание 2"
        );
    }

    [Fact]
    public void ParseEvents_WhenActualLineSeparatorWithWhitespace_ReturnsOnlyAbove()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1
                      
                      		actual line:===============================================================

                      25.03.2026 10:00 событие 2 (не должно парситься)
                      Описание 2
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Single(result);
        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );
    }

    [Fact]
    public void ParseEvents_WhenActualLineSeparatorWithExtraText_ReturnsOnlyAbove()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1

                      actual line:=============================================================== (доп текст)

                      25.03.2026 10:00 событие 2 (не должно парситься)
                      Описание 2
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Single(result);
        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );
    }

    [Fact]
    public void ParseEvents_WhenNoActualLineSeparator_ReturnsAllEvents()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1

                      25.03.2026 10:00 событие 2
                      Описание 2
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Equal(2, result.Count);

        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );

        Helpers.AssertEventData(
            actual: result[1],
            expectedTime: new DateTime(2026, 3, 25, 10, 0, 0),
            expectedSubject: "событие 2",
            expectedDescription: "Описание 2"
        );
    }

    [Fact]
    public void ParseEvents_WhenEmptyLinesBeforeActualLineSeparator_ReturnsOnlyAbove()
    {
        var content = """
                      24.03.2026 22:00 событие 1
                      Описание 1


                      actual line:===============================================================

                      25.03.2026 10:00 событие 2 (не должно парситься)
                      Описание 2
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Single(result);
        Helpers.AssertEventData(
            actual: result[0],
            expectedTime: new DateTime(2026, 3, 24, 22, 0, 0),
            expectedSubject: "событие 1",
            expectedDescription: "Описание 1"
        );
    }

    [Fact]
    public void ParseEvents_WhenTextWithWhitespaceOnly_ReturnsEmptyList()
    {
        var content = """
                              	
                      """;

        var result = _parser.ParseEvents(content);

        Assert.Empty(result);
    }
}
