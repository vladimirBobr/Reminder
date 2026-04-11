using ReminderApp.Common;

namespace ReminderApp.EventParsing;

/// <summary>Парсер файла событий</summary>
public class FileParser
{
    public FileParsingResult ParseFile(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new() { DateSections = [], DifferentDates = null, NotesSection = null };

        var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        // 1. Найти все заголовки с их индексами и типами
        var headers = new List<(int Index, SectionType Type, DateOnly? Date)>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            (bool IsHeader, SectionType Type, DateOnly? Date) header = TryParseHeader(lines[i]);
            if (header.IsHeader)
                headers.Add((i, header.Type, header.Date));
        }

        // 2. Для каждого заголовка собрать контент до следующего
        var dateSections = new List<DateSection>();
        DifferentDatesSection? diffDates = null;
        NotesSection? notes = null;

        for (int h = 0; h < headers.Count; h++)
        {
            var current = headers[h];
            var nextIndex = h + 1 < headers.Count ? headers[h + 1].Index : lines.Length;

            // Собрать строки между текущим и следующим заголовком
            var contentLines = new List<string>();
            for (int i = current.Index + 1; i < nextIndex; i++)
                contentLines.Add(lines[i]);

            var blocks = ToBlocks(contentLines);

            if (current.Type == SectionType.Date && current.Date.HasValue)
            {
                dateSections.Add(new DateSection { Date = current.Date.Value, EventBlocks = blocks });
            }
            else if (current.Type == SectionType.DifferentDates)
            {
                diffDates = new DifferentDatesSection { EventBlocks = blocks };
            }
            else if (current.Type == SectionType.Notes)
            {
                notes = new NotesSection { EventBlocks = blocks };
            }
        }

        return new()
        {
            DateSections = dateSections,
            DifferentDates = diffDates,
            NotesSection = notes
        };
    }

    private (bool IsHeader, SectionType Type, DateOnly? Date) TryParseHeader(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return (false, default, null);

        // Найти все # в строке
        var hashIndices = new List<int>();
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '#')
                hashIndices.Add(i);
        }

        // Нужно минимум 2 #
        if (hashIndices.Count < 2)
            return (false, default, null);

        // Берём контент между первым и последним #
        var firstHash = hashIndices[0];
        var lastHash = hashIndices[^1];
        
        if (lastHash <= firstHash + 1)
            return (false, default, null);

        var content = line[(firstHash + 1)..lastHash].Trim();
        if (string.IsNullOrEmpty(content))
            return (false, default, null);

        // Проверяем дату
        if (DateOnly.TryParseExact(content, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
            return (true, SectionType.Date, date);

        // Проверяем different_dates_section
        var lower = content.ToLowerInvariant();
        if (lower == "different_dates_section")
            return (true, SectionType.DifferentDates, null);

        // Проверяем notes_section
        if (lower == "notes_section")
            return (true, SectionType.Notes, null);

        return (false, default, null);
    }

    private List<string> ToBlocks(List<string> lines)
    {
        var blocks = new List<string>();
        var sb = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0)
                {
                    blocks.Add(sb.ToString().Trim());
                    sb.Clear();
                }
            }
            else
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(line);
            }
        }

        if (sb.Length > 0) blocks.Add(sb.ToString().Trim());
        return blocks;
    }

    internal List<EventData> ParseEvents(string content) => throw new NotImplementedException();

    private enum SectionType { Date, DifferentDates, Notes }
}
