using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public partial class FileParserTests
{
    private readonly FileParser _parser = new();

    [Fact]
    public void ParseFile_WhenEmptyString_ReturnsEmptyResult()
    {
        var result = _parser.ParseFile("");

        Assert.NotNull(result);
        Assert.Empty(result.DateSections);
        Assert.Null(result.DifferentDates);
        Assert.Null(result.NotesSection);
    }

    [Fact]
    public void ParseFile_WhenWhitespaceOnly_ReturnsEmptyResult()
    {
        var result = _parser.ParseFile("   \n\t\r   ");

        Assert.NotNull(result);
        Assert.Empty(result.DateSections);
    }

    [Fact]
    public void ParseFile_WhenSingleDateSection_ReturnsOneSection()
    {
        var content = """
            # 10.04.2026 #

            событие 1

            событие 2
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026")
            .WithBlocks("событие 1", "событие 2")
            .AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WhenMultipleDateSections_ReturnsAllSections()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # 11.04.2026 #
            событие на субботу

            # 12.04.2026 #
            событие на воскресенье
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks("событие на пятницу").AssertMatches(result);
        Expect.DateSection("11.04.2026").WithBlocks("событие на субботу").AssertMatches(result);
        Expect.DateSection("12.04.2026").WithBlocks("событие на воскресенье").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithDifferentDatesSection_ReturnsIt()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # different_dates_section #

            событие с датой 02.04.2026

            10.04.2026 Пт Дежурный ФТ
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks("событие на пятницу").AssertMatches(result);
        Expect.DifferentDates()
            .WithBlocks("событие с датой 02.04.2026", "10.04.2026 Пт Дежурный ФТ")
            .AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithNotesSection_ReturnsIt()
    {
        var content = """
            # notes_section #
            моя заметка 1

            моя заметка 2
            """;

        var result = _parser.ParseFile(content);

        Expect.Notes()
            .WithBlocks("моя заметка 1", "моя заметка 2")
            .AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WhenEventBlocksSeparatedByEmptyLines_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #

            первое событие
            многострочное


            второе событие



            третье событие
            """;
        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026")
            .WithBlocks(
                "первое событие" + Environment.NewLine + "многострочное",
                "второе событие",
                "третье событие"
            )
            .AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WhenSectionWithoutEvents_ReturnsEmptyBlocks()
    {
        var content = """
            # 10.04.2026 #

            # 11.04.2026 #
            событие есть
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").AssertBlockCount(result, 0);
        Expect.DateSection("11.04.2026").WithBlocks("событие есть").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WhenNoHeaders_ReturnsEmptyDateSections()
    {
        var content = """
            просто текст без заголовков
            ещё текст
            """;

        var result = _parser.ParseFile(content);

        Assert.Empty(result.DateSections);
        Assert.Null(result.DifferentDates);
    }

    [Fact]
    public void ParseFile_WithAllThreeSections_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # different_dates_section #

            02.04.2026 Чт Дежурный ФТ

            # notes_section #

            заметка для себя
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks("событие на пятницу").AssertMatches(result);
        Expect.DifferentDates().WithBlocks("02.04.2026 Чт Дежурный ФТ").AssertMatches(result);
        Expect.Notes().WithBlocks("заметка для себя").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithVariousHeaderFormats_ParsesCorrectly()
    {
        var content = """
            # 12.04.2026 #
            событие

            # different_dates_section #
            другое событие

            # notes_section #
            заметка
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("12.04.2026").WithBlocks("событие").AssertMatches(result);
        Expect.DifferentDates().WithBlocks("другое событие").AssertMatches(result);
        Expect.Notes().WithBlocks("заметка").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithManyEquals_ParsesCorrectly()
    {
        var content = """
            ====================# 12.04.2026 #=====================
            событие
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("12.04.2026").WithBlocks("событие").AssertMatches(result);
    }

    // =============== Краевые сценарии ===============

    [Fact]
    public void ParseFile_WithDateWithoutClosingHash_ParsesCorrectly()
    {
        // Note: with # format, we need # on both sides for proper parsing
        // This test verifies that incomplete headers (only one #) are not recognized
        var content = """
            # 10.04.2026
            событие
            """;

        var result = _parser.ParseFile(content);

        // Without closing #, the line is not recognized as a header
        Assert.Empty(result.DateSections);
    }

    [Fact]
    public void ParseFile_WithLeadingZerosInDate_ParsesCorrectly()
    {
        var content = """
            # 01.01.2026 #
            новогоднее событие
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("01.01.2026").WithBlocks("новогоднее событие").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithEmptyLinesAfterHeader_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #



            событие после пустых строк
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks("событие после пустых строк").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithSpecialCharactersInEvent_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            Событие с <тегами> и "кавычками"
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks("Событие с <тегами> и \"кавычками\"").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithRussianText_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            событие с русским текстом и русскими буквами
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks("событие с русским текстом и русскими буквами").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithOnlyEqualsSigns_NoSections()
    {
        var content = """
            #
            текст без даты
            """;

        var result = _parser.ParseFile(content);

        Assert.Empty(result.DateSections);
        Assert.Null(result.DifferentDates);
    }

    [Fact]
    public void ParseFile_WithSectionAfterDifferentDates_ParsesCorrectly()
    {
        var content = """
            # different_dates_section #
            разные даты

            # 10.04.2026 #
            событие на дату
            """;

        var result = _parser.ParseFile(content);

        Expect.DifferentDates().WithBlocks("разные даты").AssertMatches(result);
        Expect.DateSection("10.04.2026").WithBlocks("событие на дату").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithNotesFirstThenDate_ParsesCorrectly()
    {
        var content = """
            # notes_section #
            заметка

            # 10.04.2026 #
            событие
            """;

        var result = _parser.ParseFile(content);

        Expect.Notes().WithBlocks("заметка").AssertMatches(result);
        Expect.DateSection("10.04.2026").WithBlocks("событие").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithVeryLongLine_ParsesCorrectly()
    {
        var veryLongText = new string('а', 10000);
        var content = $"""
            # 10.04.2026 #
            {veryLongText}
            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").WithBlocks(veryLongText).AssertMatches(result);
    }

    [Fact]
    public void ParseFile_WithMultipleEmptySections_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #

            # 11.04.2026 #

            # 12.04.2026 #

            """;

        var result = _parser.ParseFile(content);

        Expect.DateSection("10.04.2026").AssertBlockCount(result, 0);
        Expect.DateSection("11.04.2026").AssertBlockCount(result, 0);
        Expect.DateSection("12.04.2026").AssertBlockCount(result, 0);
    }

    [Fact]
    public void ParseFile_DifferentDatesSectionIsCaseInsensitive()
    {
        var content = """
            # DIFFERENT_DATES_SECTION #
            событие
            """;

        var result = _parser.ParseFile(content);

        Expect.DifferentDates().WithBlocks("событие").AssertMatches(result);
    }

    [Fact]
    public void ParseFile_NotesSectionIsCaseInsensitive()
    {
        var content = """
            # NOTES_SECTION #
            заметка
            """;

        var result = _parser.ParseFile(content);

        Expect.Notes().WithBlocks("заметка").AssertMatches(result);
    }
}
