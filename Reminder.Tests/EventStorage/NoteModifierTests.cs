using OneOf;
using ReminderApp.EventStorage;
using Xunit.Abstractions;

namespace Reminder.Tests.EventStorage;

/// <summary>
/// Test helper for NoteModifierTests
/// </summary>
public static class NoteModifierTestHelper
{
    /// <summary>
    /// Returns only ModifiedContent. Reads as: "note text".AddToModified(content)
    /// </summary>
    public static string? AddToModified(this string note, string content, string? dateStr = null)
    {
        DateOnly? date = dateStr != null ? DateOnly.ParseExact(dateStr, "dd.MM.yyyy") : null;
        var result = NoteModifier.ModifyContent(content, note, date);
        return result.Match<string?>(
            error => null,
            success => success.ModifiedContent
        );
    }

    /// <summary>
    /// Writes expected and modified for comparison
    /// </summary>
    public static void WriteComparison(ITestOutputHelper output, string expected, string? modified)
    {
        output.WriteLine($"EXPECTED:{Environment.NewLine}{expected}");
        output.WriteLine($"MODIFIED:{Environment.NewLine}{modified}");
    }

    /// <summary>
    /// Normalizes line endings and trims trailing newlines for comparison
    /// </summary>
    private static string Normalize(string? s) =>
        s!.Replace("\r\n", "\n").TrimEnd('\n');

    /// <summary>
    /// Asserts expected equals modified after normalization
    /// </summary>
    public static void AssertEqual(string expected, string? modified)
    {
        Assert.Equal(Normalize(expected), Normalize(modified));
    }
}

public class NoteModifierTests
{
    private readonly ITestOutputHelper _output;

    public NoteModifierTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // =============== notes_section (без даты) ===============

    [Fact]
    public void ModifyContent_NoDate_EmptyNotesSection_AddsNote()
    {
        var content = """
            #notes_section#
            """;
        
        var modified = "новая заметка".AddToModified(content);
        
        var expected = """
            #notes_section#

            новая заметка
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_NoDate_NotesSectionWithContent_InsertsBeforeExisting()
    {
        var content = """
            #notes_section#
            существующая заметка
            """;
        
        var modified = "новая заметка".AddToModified(content);
        
        var expected = """
            #notes_section#

            новая заметка

            существующая заметка
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_NoDate_NotesSectionNotFound_AddsSectionAtEnd()
    {
        var content = """
            #10.04.2026#
            событие
            """;
        
        var modified = "новая заметка".AddToModified(content);
        
        var expected = """
            #10.04.2026#
            событие

            #notes_section#

            новая заметка
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_NoDate_MultilineNote_AddsCorrectly()
    {
        var content = """
            #notes_section#
            """;
        
        var modified = """
            первая строка
            вторая строка
            третья строка
            """.AddToModified(content);

        var expected = """
            #notes_section#

            первая строка
            вторая строка
            третья строка
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    // =============== Date section ===============

    [Fact]
    public void ModifyContent_WithDate_DateSectionExists_InsertsBeforeExisting()
    {
        var content = """
            #10.04.2026#
            первое событие

            #11.04.2026#
            второе событие
            """;
        
        var modified = "новое событие".AddToModified(content, "10.04.2026");
        
        var expected = """
            #10.04.2026#

            новое событие

            первое событие

            #11.04.2026#
            второе событие
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_DateSectionWithPrefix_FindsSection()
    {
        var content = """
            СРЕДА #22.04.2026#
            existing note
            """;
        
        var modified = "🏃‍♂️ 10км легкий бег".AddToModified(content, "22.04.2026");
        
        var expected = """
            СРЕДА #22.04.2026#

            🏃‍♂️ 10км легкий бег

            existing note
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_MultipleDateSections_FindsCorrectSection()
    {
        var content = """
            #09.04.2026#
            first day

            СРЕДА #22.04.2026#
            middle day

            #30.04.2026#
            last day
            """;
        
        var modified = "wednesday note".AddToModified(content, "22.04.2026");
        
        var expected = """
            #09.04.2026#
            first day

            СРЕДА #22.04.2026#

            wednesday note

            middle day

            #30.04.2026#
            last day
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_DateSectionAtEndOfFile_InsertsBeforeExisting()
    {
        var content = """
            #10.04.2026#
            последнее событие
            """;
        
        var modified = "новое".AddToModified(content, "10.04.2026");
        
        var expected = """
            #10.04.2026#

            новое

            последнее событие
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    // =============== different_dates_section ===============

    [Fact]
    public void ModifyContent_WithDate_DifferentDatesSectionExists_InsertsInDateOrder()
    {
        var content = """
            #different_dates_section#
            05.04.2026 существующее
            """;
        
        var modified = "new note".AddToModified(content, "25.04.2026");
        
        var expected = """
            #different_dates_section#
            05.04.2026 существующее

            25.04.2026 new note

            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_DifferentDatesSectionNotFound_AddsSectionAtEnd()
    {
        var content = """
            #10.04.2026#
            событие
            """;
        
        var modified = "note".AddToModified(content, "15.04.2026");
        
        var expected = """
            #10.04.2026#
            событие

            #different_dates_section#

            15.04.2026 note
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        Assert.NotNull(modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_EmptyDifferentDatesSection_AddsEntry()
    {
        var content = """
            #different_dates_section#
            """;
        
        var modified = "first entry".AddToModified(content, "15.04.2026");
        
        var expected = """
            #different_dates_section#

            15.04.2026 first entry
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_DifferentDatesSection_MaintainsAscendingOrder_InsertAtBeginning()
    {
        var content = """
            #different_dates_section#
            05.04.2026 earliest

            20.04.2026 middle

            30.04.2026 latest
            """;
        
        var modified = "new earliest".AddToModified(content, "01.04.2026");
        
        var expected = """
            #different_dates_section#

            01.04.2026 new earliest

            05.04.2026 earliest

            20.04.2026 middle

            30.04.2026 latest
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_DifferentDatesSection_MaintainsAscendingOrder_InsertInMiddle()
    {
        var content = """
            #different_dates_section#
            01.04.2026 early

            20.04.2026 late
            """;
        
        var modified = "middle entry".AddToModified(content, "15.04.2026");
        
        var expected = """
            #different_dates_section#
            01.04.2026 early

            15.04.2026 middle entry


            20.04.2026 late
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    // =============== Case sensitivity ===============

    [Fact]
    public void ModifyContent_NoDate_CaseInsensitive_SectionFound()
    {
        var content = """
            #NOTES_SECTION#
            old note
            """;
        
        var modified = "new note".AddToModified(content);
        
        var expected = """
            #NOTES_SECTION#
            old note

            #notes_section#

            new note
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    [Fact]
    public void ModifyContent_WithDate_DifferentDatesSection_CaseInsensitive()
    {
        var content = """
            #DIFFERENT_DATES_SECTION#
            10.04.2026 existing
            """;
        
        var modified = "new".AddToModified(content, "15.04.2026");
        
        var expected = """
            #DIFFERENT_DATES_SECTION#
            10.04.2026 existing

            #different_dates_section#

            15.04.2026 new
            """;
        
        NoteModifierTestHelper.WriteComparison(_output, expected, modified);
        NoteModifierTestHelper.AssertEqual(expected, modified);
    }

    // =============== Error cases ===============

    [Fact]
    public void ModifyContent_EmptyNote_ThrowsArgumentException()
    {
        var content = """
            #notes_section#
            """;
        
        Assert.Throws<ArgumentException>(() =>
            "".AddToModified(content));
    }

    [Fact]
    public void ModifyContent_WhitespaceOnlyNote_ThrowsArgumentException()
    {
        var content = """
            #notes_section#
            """;
        
        Assert.Throws<ArgumentException>(() =>
            "   ".AddToModified(content));
    }
}