using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

public class FileParserTests
{
    private readonly FileParser _parser = new();

    // =============== Базовые сценарии ParseFile ===============

    [Fact]
    public void ParseFile_WhenEmptyString_ReturnsEmptyResult()
    {
        var result = _parser.ParseFile("");

        Assert.Empty(result.DateSections);
        Assert.Null(result.DifferentDates);
        Assert.Null(result.NotesSection);
    }

    [Fact]
    public void ParseFile_WhenWhitespaceOnly_ReturnsEmptyResult()
    {
        var result = _parser.ParseFile("   \n\t\r   ");

        Assert.Empty(result.DateSections);
        Assert.Null(result.DifferentDates);
        Assert.Null(result.NotesSection);
    }

    [Fact]
    public void ParseFile_WhenNoHeaders_ReturnsEmptyResult()
    {
        var content = "просто текст без заголовков\nещё текст";
        var result = _parser.ParseFile(content);

        Assert.Empty(result.DateSections);
        Assert.Null(result.DifferentDates);
        Assert.Null(result.NotesSection);
    }

    [Fact]
    public void ParseFile_WhenSingleDateSection_ReturnsCorrectStructure()
    {
        var content = """
            # 10.04.2026 #

            событие 1

            событие 2
            """;
        var result = _parser.ParseFile(content);

        Assert.Single(result.DateSections);
        var section = result.DateSections[0];
        
        Assert.Equal(new DateOnly(2026, 4, 10), section.Date);
        Assert.Equal(2, section.Events.Count);
        Assert.Equal("событие 1", section.Events[0].Event.Subject);
        Assert.Equal("событие 2", section.Events[1].Event.Subject);
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

        Assert.Equal(3, result.DateSections.Count);
        
        Assert.Equal(new DateOnly(2026, 4, 10), result.DateSections[0].Date);
        Assert.Equal(new DateOnly(2026, 4, 11), result.DateSections[1].Date);
        Assert.Equal(new DateOnly(2026, 4, 12), result.DateSections[2].Date);
    }

    [Fact]
    public void ParseFile_WhenDifferentDatesSection_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            событие на пятницу

            # different_dates_section #

            02.04.2026 Чт Дежурный ФТ

            10.04.2026 Пт Дежурный ФТ
            """;
        var result = _parser.ParseFile(content);

        Assert.Single(result.DateSections);
        Assert.NotNull(result.DifferentDates);
        Assert.Equal(2, result.DifferentDates.Events.Count);
        
        Assert.Equal(new DateOnly(2026, 4, 2), result.DifferentDates.Events[0].Event.Date);
        Assert.Equal("Чт Дежурный ФТ", result.DifferentDates.Events[0].Event.Subject);
        Assert.Equal(new DateOnly(2026, 4, 10), result.DifferentDates.Events[1].Event.Date);
        Assert.Equal("Пт Дежурный ФТ", result.DifferentDates.Events[1].Event.Subject);
    }

    [Fact]
    public void ParseFile_WhenNotesSection_ParsesCorrectly()
    {
        var content = """
            # notes_section #

            заметка 1

            заметка 2
            """;
        var result = _parser.ParseFile(content);

        Assert.NotNull(result.NotesSection);
        Assert.Equal(2, result.NotesSection.Events.Count);
        Assert.Equal("заметка 1", result.NotesSection.Events[0].Event.Subject);
        Assert.Equal("заметка 2", result.NotesSection.Events[1].Event.Subject);
        // NotesSection использует DateOnly.MinValue как маркер
        Assert.Equal(DateOnly.MinValue, result.NotesSection.Events[0].Event.Date);
    }

    [Fact]
    public void ParseFile_WhenAllSections_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            событие на дату

            # different_dates_section #
            05.04.2026 другое событие

            # notes_section #
            заметка
            """;
        var result = _parser.ParseFile(content);

        Assert.Single(result.DateSections);
        Assert.NotNull(result.DifferentDates);
        Assert.NotNull(result.NotesSection);
    }

    // =============== Тесты индексов строк ===============

    [Fact]
    public void ParseFile_WhenDateSection_CorrectLineIndices()
    {
        // Content is: "# 10.04.2026 #\n\nсобытие" = 3 lines
        // Line 0: # 10.04.2026 #
        // Line 1: (empty)
        // Line 2: событие
        var content = "# 10.04.2026 #\n\nсобытие";
        var result = _parser.ParseFile(content);
        var section = result.DateSections[0];

        Assert.Equal(0, section.HeaderLineIndex);
        Assert.Equal(1, section.ContentStartLineIndex);
        Assert.Equal(2, section.ContentEndLineIndex); // nextIndex(3) - 1
        Assert.Equal(2, section.Events[0].StartLineIndex);
        Assert.Equal(2, section.Events[0].EndLineIndex);
    }

    [Fact]
    public void ParseFile_WhenMultilineEvent_CorrectLineIndices()
    {
        // "# 10.04.2026 #\n18:30 Встреча\nСтрока 1 описания\nСтрока 2 описания" = 4 lines
        // Line 0: # 10.04.2026 #
        // Line 1: 18:30 Встреча
        // Line 2: Строка 1 описания
        // Line 3: Строка 2 описания
        var content = "# 10.04.2026 #\n18:30 Встреча\nСтрока 1 описания\nСтрока 2 описания";
        var result = _parser.ParseFile(content);
        var section = result.DateSections[0];
        var parsedEvent = section.Events[0];

        Assert.Equal(1, parsedEvent.StartLineIndex);
        Assert.Equal(3, parsedEvent.EndLineIndex);
        Assert.Equal("Встреча", parsedEvent.Event.Subject);
        Assert.Equal("Строка 1 описания" + Environment.NewLine + "Строка 2 описания", parsedEvent.Event.Description);
    }

    [Fact]
    public void ParseFile_WhenEmptySections_CorrectLineIndices()
    {
        // "# 10.04.2026 #\n\n# 11.04.2026 #" = 3 lines (split by \n)
        // Line 0: # 10.04.2026 #
        // Line 1: (empty)
        // Line 2: # 11.04.2026 #
        var content = "# 10.04.2026 #\n\n# 11.04.2026 #";
        var result = _parser.ParseFile(content);

        Assert.Equal(2, result.DateSections.Count);
        
        // Первая секция: header=0, nextHeader=2, so contentStart=1, contentEnd=1
        Assert.Equal(0, result.DateSections[0].HeaderLineIndex);
        Assert.Equal(1, result.DateSections[0].ContentStartLineIndex);
        Assert.Equal(1, result.DateSections[0].ContentEndLineIndex);
        Assert.Empty(result.DateSections[0].Events);

        // Вторая секция: header=2, no next header, so nextIndex=3 (lines.Length), contentStart=3, contentEnd=2
        Assert.Equal(2, result.DateSections[1].HeaderLineIndex);
        Assert.Equal(3, result.DateSections[1].ContentStartLineIndex);
        Assert.Equal(2, result.DateSections[1].ContentEndLineIndex);
        Assert.Empty(result.DateSections[1].Events);
    }

    [Fact]
    public void ParseFile_WhenDifferentDatesSection_CorrectLineIndices()
    {
        // "# different_dates_section #\n\n02.04.2026 Чт Дежурный ФТ" = 3 lines
        // Line 0: # different_dates_section #
        // Line 1: (empty)
        // Line 2: 02.04.2026 Чт Дежурный ФТ
        var content = "# different_dates_section #\n\n02.04.2026 Чт Дежурный ФТ";
        var result = _parser.ParseFile(content);
        var section = result.DifferentDates!;

        Assert.Equal(0, section.HeaderLineIndex);
        Assert.Equal(1, section.ContentStartLineIndex);
        Assert.Equal(2, section.ContentEndLineIndex); // nextIndex(3) - 1
        Assert.Equal(2, section.Events[0].StartLineIndex);
        Assert.Equal(2, section.Events[0].EndLineIndex);
    }

    [Fact]
    public void ParseFile_WhenNotesSection_CorrectLineIndices()
    {
        // "# notes_section #\n\nпервая заметка\n\nвторая заметка" = 5 lines
        // Line 0: # notes_section #
        // Line 1: (empty)
        // Line 2: первая заметка
        // Line 3: (empty)
        // Line 4: вторая заметка
        var content = "# notes_section #\n\nпервая заметка\n\nвторая заметка";
        var result = _parser.ParseFile(content);
        var section = result.NotesSection!;

        Assert.Equal(0, section.HeaderLineIndex);
        Assert.Equal(1, section.ContentStartLineIndex);
        Assert.Equal(4, section.ContentEndLineIndex);
        Assert.Equal(2, section.Events[0].StartLineIndex);
        Assert.Equal(2, section.Events[0].EndLineIndex);
        Assert.Equal(4, section.Events[1].StartLineIndex);
        Assert.Equal(4, section.Events[1].EndLineIndex);
    }

    // =============== Краевые сценарии ===============

    [Fact]
    public void ParseFile_WhenSectionWithoutEvents_ReturnsEmptyEventsList()
    {
        var content = """
            # 10.04.2026 #

            # 11.04.2026 #
            событие есть
            """;
        var result = _parser.ParseFile(content);

        Assert.Equal(2, result.DateSections.Count);
        Assert.Empty(result.DateSections[0].Events);
        Assert.Single(result.DateSections[1].Events);
    }

    [Fact]
    public void ParseFile_WhenSectionAtEndOfFile_CorrectIndices()
    {
        // "# notes_section #\nзаметка" = 2 lines
        // Line 0: # notes_section #
        // Line 1: заметка
        var content = "# notes_section #\nзаметка";
        var result = _parser.ParseFile(content);
        var section = result.NotesSection!;

        Assert.Equal(0, section.HeaderLineIndex);
        Assert.Equal(1, section.ContentStartLineIndex);
        Assert.Equal(1, section.ContentEndLineIndex);
    }

    [Fact]
    public void ParseFile_WhenOnlyWhitespaceBetweenHeaders_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #


            # 11.04.2026 #
            """;
        var result = _parser.ParseFile(content);

        Assert.Equal(2, result.DateSections.Count);
        Assert.Empty(result.DateSections[0].Events);
        Assert.Empty(result.DateSections[1].Events);
    }

    [Fact]
    public void ParseFile_WhenDifferentDatesCaseInsensitive_ParsesCorrectly()
    {
        var content = """
            # DIFFERENT_DATES_SECTION #
            02.04.2026 событие
            """;
        var result = _parser.ParseFile(content);

        Assert.NotNull(result.DifferentDates);
        Assert.Single(result.DifferentDates.Events);
    }

    [Fact]
    public void ParseFile_WhenNotesCaseInsensitive_ParsesCorrectly()
    {
        var content = """
            # NOTES_SECTION #
            заметка
            """;
        var result = _parser.ParseFile(content);

        Assert.NotNull(result.NotesSection);
        Assert.Single(result.NotesSection.Events);
    }

    [Fact]
    public void ParseFile_WhenEmptyDifferentDatesSection_ReturnsEmptyEvents()
    {
        var content = """
            # different_dates_section #
            """;
        var result = _parser.ParseFile(content);

        Assert.NotNull(result.DifferentDates);
        Assert.Empty(result.DifferentDates.Events);
    }

    [Fact]
    public void ParseFile_WhenEmptyNotesSection_ReturnsEmptyEvents()
    {
        var content = """
            # notes_section #
            """;
        var result = _parser.ParseFile(content);

        Assert.NotNull(result.NotesSection);
        Assert.Empty(result.NotesSection.Events);
    }

    [Fact]
    public void ParseFile_WhenDateSectionWithMultipleEvents_PreservesOrder()
    {
        var content = """
            # 10.04.2026 #
            событие 1

            событие 2

            событие 3
            """;
        var result = _parser.ParseFile(content);
        var section = result.DateSections[0];

        Assert.Equal(3, section.Events.Count);
        Assert.Equal("событие 1", section.Events[0].Event.Subject);
        Assert.Equal("событие 2", section.Events[1].Event.Subject);
        Assert.Equal("событие 3", section.Events[2].Event.Subject);
    }

    [Fact]
    public void ParseFile_WhenDateSectionWithTime_ParsesCorrectly()
    {
        var content = """
            # 10.04.2026 #
            18:30 Встреча с клиентом
            """;
        var result = _parser.ParseFile(content);
        var section = result.DateSections[0];

        Assert.Single(section.Events);
        Assert.Equal(new TimeOnly(18, 30), section.Events[0].Event.Time);
        Assert.Equal("Встреча с клиентом", section.Events[0].Event.Subject);
    }

    [Fact]
    public void ParseFile_WhenDifferentDatesWithTime_ParsesCorrectly()
    {
        var content = """
            # different_dates_section #
            10.04.2026 14:00 Встреча
            """;
        var result = _parser.ParseFile(content);
        var section = result.DifferentDates!;

        Assert.Single(section.Events);
        Assert.Equal(new DateOnly(2026, 4, 10), section.Events[0].Event.Date);
        Assert.Equal(new TimeOnly(14, 0), section.Events[0].Event.Time);
        Assert.Equal("Встреча", section.Events[0].Event.Subject);
    }
}