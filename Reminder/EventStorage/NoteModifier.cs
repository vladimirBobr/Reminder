using System.Text;
using System.Text.RegularExpressions;
using OneOf;

namespace ReminderApp.EventStorage;

/// <summary>
/// Error when modifying note content
/// </summary>
public record NoteModifierError(string Message);

/// <summary>
/// Success result with modified content and message
/// </summary>
public record NoteModifierSuccess(string ModifiedContent, string ResultMessage);

/// <summary>
/// Handles the logic of inserting notes into event file content.
/// </summary>
public static class NoteModifier
{
    /// <summary>
    /// Inserts a note into the given content at the appropriate location.
    /// </summary>
    /// <param name="content">Current file content</param>
    /// <param name="note">The note text to add</param>
    /// <param name="date">Optional date - if provided and section exists, adds to that section</param>
    /// <returns>OneOf result with either NoteModifierError or NoteModifierSuccess</returns>
    public static OneOf<NoteModifierError, NoteModifierSuccess> ModifyContent(
        string content,
        string note,
        DateOnly? date = null)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("Note cannot be empty", nameof(note));
        }
        
        // Normalize line endings to \n for consistent processing
        var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalizedContent.Split('\n').ToList();
        string? resultMessage = null;
        
        if (date.HasValue)
        {
            var dateSectionPattern = $@"#.*{date.Value:dd\.MM\.yyyy}.*#";
            
            // Try to find existing date section first
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (Regex.IsMatch(line, dateSectionPattern, RegexOptions.IgnoreCase))
                {
                    // Found the date section at line i
                    // Find the END of this section (before next header or end of file)
                    var endOfSection = FindEndOfSection(lines, i + 1);
                    
                    // Insert note BEFORE existing content (at the beginning of section)
                    lines.Insert(i + 1, "");
                    lines.Insert(i + 2, note);
                    lines.Insert(i + 3, "");
                    resultMessage = "Добавили в существующую секцию";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Date section not found - add to different_dates_section
                var sectionFound = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("#different_dates_section#"))
                    {
                        // Find the last non-empty line index (or end of section before next header)
                        var lastContentIndex = FindLastContentIndex(lines, i + 1);
                        
                        // Insert note AFTER existing content, maintaining date order
                        var insertIndex = FindInsertIndexForDate(lines, lastContentIndex, date.Value);
                        
                        // Add empty line before new entry
                        lines.Insert(insertIndex, "");
                        lines.Insert(insertIndex + 1, $"{date.Value:dd.MM.yyyy} {note}");
                        lines.Insert(insertIndex + 2, "");
                        sectionFound = true;
                        break;
                    }
                }

                if (!sectionFound)
                {
                    // Add #different_dates_section# at end of file
                    lines.Add("");
                    lines.Add("#different_dates_section#");
                    lines.Add("");
                    lines.Add($"{date.Value:dd.MM.yyyy} {note}");
                    lines.Add("");
                    resultMessage = "Добавили в #different_dates_section#";
                }
                else
                {
                    resultMessage = "Добавили в #different_dates_section#";
                }
            }
        }
        else
        {
            // No date - add to notes_section
            var sectionFound = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("#notes_section#"))
                {
                    // Find last content index after header
                    var lastContentIndex = FindLastContentIndex(lines, i + 1);
                    
                    // Insert note BEFORE existing content (after header line)
                    if (lastContentIndex > i)
                    {
                        // There is existing content, insert before it
                        lines.Insert(i + 1, "");
                        lines.Insert(i + 2, note);
                        lines.Insert(i + 3, "");
                    }
                    else
                    {
                        // No existing content, add after header
                        lines.Insert(i + 1, "");
                        lines.Insert(i + 2, note);
                        lines.Insert(i + 3, "");
                    }
                    sectionFound = true;
                    break;
                }
            }

            if (!sectionFound)
            {
                // Add #notes_section# at end of file
                lines.Add("");
                lines.Add("#notes_section#");
                lines.Add("");
                lines.Add(note);
                lines.Add("");
            }
            resultMessage = "Добавили в #notes_section#";
        }

        var newContent = string.Join("\n", lines);
        return new NoteModifierSuccess(newContent, resultMessage!);
    }

    private static int FindEndOfSection(List<string> lines, int startIndex)
    {
        for (int i = startIndex; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            
            if (line.StartsWith('#') && line.EndsWith('#') && line.Length > 2)
            {
                return i;
            }
        }
        
        return lines.Count;
    }

    /// <summary>
    /// Finds the last non-empty content line index, stopping at section headers.
    /// Returns index of last non-empty line, or startIndex - 1 if none found.
    /// </summary>
    private static int FindLastContentIndex(List<string> lines, int startIndex)
    {
        var lastIndex = startIndex - 1;
        
        for (int i = startIndex; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            
            // Stop if we hit a section header
            if (trimmed.StartsWith('#') && trimmed.EndsWith('#') && trimmed.Length > 2)
            {
                break;
            }
            
            // Track non-empty lines
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                lastIndex = i;
            }
        }
        
        return lastIndex;
    }
    
    /// <summary>
    /// Finds the correct insert index for a new date entry to maintain date order.
    /// Returns the index where the new date entry should be inserted.
    /// </summary>
    private static int FindInsertIndexForDate(List<string> lines, int lastContentIndex, DateOnly newDate)
    {
        // Check lines from lastContentIndex going backwards to find insertion point
        for (int i = lastContentIndex; i >= 0; i--)
        {
            var line = lines[i].Trim();
            
            // Stop if we hit a section header
            if (line.StartsWith('#') && line.EndsWith('#') && line.Length > 2)
            {
                // We've gone past the section header, insert after all existing content
                return i + 1;
            }
            
            // Try to parse existing date entry
            var existingDate = TryParseDateEntry(line);
            if (existingDate.HasValue)
            {
                if (newDate < existingDate.Value)
                {
                    // New date is earlier, continue looking for insertion point before this entry
                    continue;
                }
                else
                {
                    // New date is >= existing date, insert after it
                    return i + 1;
                }
            }
        }
        
        // No existing dates found, insert at the position after section header
        return lastContentIndex + 1;
    }
    
    /// <summary>
    /// Tries to parse a date from a line in different_dates_section format.
    /// Expected format: "dd.MM.yyyy some text"
    /// </summary>
    private static DateOnly? TryParseDateEntry(string line)
    {
        var parts = line.Split(' ', 2);
        if (parts.Length > 0 && DateOnly.TryParseExact(parts[0], "dd.MM.yyyy", out var date))
        {
            return date;
        }
        return null;
    }
}