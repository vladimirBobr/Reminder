using System.Text;
using System.Text.RegularExpressions;
using OneOf;

namespace ReminderApp.EventStorage;

public record NoteModifierError(string Message);
public record NoteModifierSuccess(string ModifiedContent, string ResultMessage);

public static class NoteModifier
{
    public static OneOf<NoteModifierError, NoteModifierSuccess> ModifyContent(
        string content,
        string note,
        DateOnly? date = null)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Note cannot be empty", nameof(note));

        var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalizedContent.Split('\n').ToList();
        string? resultMessage = null;

        if (date.HasValue)
        {
            var dateSectionPattern = $@"#.*{date.Value:dd\.MM\.yyyy}.*#";

            // Попытка найти секцию с конкретной датой
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (Regex.IsMatch(lines[i], dateSectionPattern, RegexOptions.IgnoreCase))
                {
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
                // Ищем different_dates_section
                var sectionFound = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains("#different_dates_section#"))
                    {
                        var lastContentIndex = FindLastContentIndex(lines, i + 1);
                        var insertIndex = FindInsertIndexForDate(lines, lastContentIndex, date.Value);

                        // Проверяем, нужна ли пустая строка перед новой записью
                        var needEmptyBefore = insertIndex > i + 1
                            && insertIndex > 0
                            && !string.IsNullOrWhiteSpace(lines[insertIndex - 1]);

                        // Проверяем, нужна ли пустая строка после новой записи
                        var needEmptyAfter = insertIndex < lines.Count
                            && !string.IsNullOrWhiteSpace(lines[insertIndex]);

                        if (needEmptyBefore)
                        {
                            lines.Insert(insertIndex, "");
                            insertIndex++;
                        }

                        lines.Insert(insertIndex, $"{date.Value:dd.MM.yyyy} {note}");

                        if (needEmptyAfter)
                        {
                            lines.Insert(insertIndex + 1, "");
                        }

                        sectionFound = true;
                        break;
                    }
                }

                if (!sectionFound)
                {
                    // Секция different_dates_section отсутствует — создаём в конце файла
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
            // Без даты – добавляем в notes_section
            var sectionFound = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("#notes_section#"))
                {
                    var lastContentIndex = FindLastContentIndex(lines, i + 1);
                    if (lastContentIndex > i)
                    {
                        lines.Insert(i + 1, "");
                        lines.Insert(i + 2, note);
                        lines.Insert(i + 3, "");
                    }
                    else
                    {
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
                lines.Add("");
                lines.Add("#notes_section#");
                lines.Add("");
                lines.Add(note);
                lines.Add("");
            }
            resultMessage = "Добавили в #notes_section#";
        }

        return new NoteModifierSuccess(string.Join("\n", lines), resultMessage!);
    }


    private static int FindLastContentIndex(List<string> lines, int startIndex)
    {
        var lastIndex = startIndex - 1;
        for (int i = startIndex; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('#') && trimmed.EndsWith('#') && trimmed.Length > 2)
                break;
            if (!string.IsNullOrWhiteSpace(trimmed))
                lastIndex = i;
        }
        return lastIndex;
    }

    private static int FindInsertIndexForDate(List<string> lines, int lastContentIndex, DateOnly newDate)
    {
        int? firstDateIndex = null;

        for (int i = lastContentIndex; i >= 0; i--)
        {
            var line = lines[i].Trim();

            // Остановка на заголовке секции
            if (line.StartsWith('#') && line.EndsWith('#') && line.Length > 2)
                break;

            var existingDate = TryParseDateEntry(line);
            if (existingDate.HasValue)
            {
                firstDateIndex = i; // самая верхняя (ранняя) встреченная дата
                if (existingDate <= newDate)
                {
                    // Вставляем после этой даты
                    return i + 1;
                }
            }
        }

        // Все даты больше newDate — вставляем перед самой первой
        if (firstDateIndex.HasValue)
            return firstDateIndex.Value;

        // Нет ни одной даты — вставляем после заголовка
        return lastContentIndex + 1;
    }

    private static DateOnly? TryParseDateEntry(string line)
    {
        var parts = line.Split(' ', 2);
        if (parts.Length > 0 && DateOnly.TryParseExact(parts[0], "dd.MM.yyyy", out var date))
            return date;
        return null;
    }
}
