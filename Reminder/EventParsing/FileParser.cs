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

    public List<EventData> ParseEvents(string content)
    {
        var parseResult = ParseFile(content);
        var events = new List<EventData>();

        // Обрабатываем DateSections
        foreach (var section in parseResult.DateSections)
        {
            foreach (var block in section.EventBlocks)
            {
                var eventData = ParseEventBlock(block, section.Date);
                events.Add(eventData);
            }
        }

        // Обрабатываем DifferentDatesSection - даты в самом тексте блока
        if (parseResult.DifferentDates != null)
        {
            foreach (var block in parseResult.DifferentDates.EventBlocks)
            {
                var eventData = ParseEventBlockWithDateInText(block);
                if (eventData != null)
                    events.Add(eventData);
            }
        }

        // Обрабатываем NotesSection - тоже парсим как события
        if (parseResult.NotesSection != null)
        {
            foreach (var block in parseResult.NotesSection.EventBlocks)
            {
                var eventData = ParseEventBlockWithoutDate(block);
                events.Add(eventData);
            }
        }

        return events;
    }

    /// <summary>
    /// Парсит блок из DifferentDatesSection - дата указана в самом тексте
    /// </summary>
    private EventData? ParseEventBlockWithDateInText(string block)
    {
        var lines = block.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        if (lines.Length == 0)
            return null;

        var firstLine = lines[0].Trim();

        // Ищем дату в формате dd.MM.yyyy в начале строки
        DateOnly? date = null;
        string remainingLine = firstLine;

        if (firstLine.Length >= 10)
        {
            // Пробуем найти дату в начале строки
            var dateMatch = firstLine.Substring(0, 10);
            if (DateOnly.TryParseExact(dateMatch, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                date = parsedDate;
                remainingLine = firstLine.Substring(10).Trim();
            }
        }

        if (!date.HasValue)
            return null;

        var result = new EventData { Date = date.Value };

        // Проверяем время
        if (TryParseTime(remainingLine, out var time))
        {
            result.Time = time;
            var timePrefixLength = remainingLine.IndexOf(time.ToString("HH:mm"), StringComparison.Ordinal);
            var subjectPart = remainingLine.Substring(timePrefixLength + 5).Trim();
            
            if (!string.IsNullOrEmpty(subjectPart))
            {
                result.Subject = subjectPart;
                var descriptionLines = lines.Skip(1).ToList();
                if (descriptionLines.Count > 0)
                    result.Description = string.Join(Environment.NewLine, descriptionLines);
            }
            else if (lines.Length > 1)
            {
                result.Subject = string.Join(Environment.NewLine, lines.Skip(1));
            }
        }
        else
        {
            // Нет времени - весь текст это subject
            if (lines.Length == 1)
            {
                result.Subject = remainingLine;
            }
            else
            {
                result.Subject = remainingLine;
                result.Description = string.Join(Environment.NewLine, lines.Skip(1));
            }
        }

        return result;
    }

    /// <summary>
    /// Парсит блок из NotesSection - без даты
    /// </summary>
    private EventData ParseEventBlockWithoutDate(string block)
    {
        var result = new EventData();
        var lines = block.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        if (lines.Length == 0)
            return result;

        var firstLine = lines[0].Trim();

        // Проверяем, есть ли время в начале
        if (TryParseTime(firstLine, out var time))
        {
            result.Time = time;
            var timePrefixLength = firstLine.IndexOf(time.ToString("HH:mm"), StringComparison.Ordinal);
            var remainingFirstLine = firstLine.Substring(timePrefixLength + 5).Trim();

            if (remainingFirstLine.Length > 0)
            {
                result.Subject = remainingFirstLine;
                var descriptionLines = lines.Skip(1).ToList();
                if (descriptionLines.Count > 0)
                    result.Description = string.Join(Environment.NewLine, descriptionLines);
            }
            else if (lines.Length > 1)
            {
                result.Subject = string.Join(Environment.NewLine, lines.Skip(1));
            }
        }
        else
        {
            if (lines.Length == 1)
                result.Subject = firstLine;
            else
            {
                result.Subject = firstLine;
                result.Description = string.Join(Environment.NewLine, lines.Skip(1));
            }
        }

        return result;
    }

    private EventData ParseEventBlock(string block, DateOnly date)
    {
        var result = new EventData { Date = date };
        var lines = block.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        if (lines.Length == 0)
            return result;

        var firstLine = lines[0].Trim();

        // Проверяем, начинается ли первая строка со времени в формате "HH:mm"
        if (TryParseTime(firstLine, out var time))
        {
            result.Time = time;
            // Остаток первой строки после времени - это начало текста
            var timePrefixLength = firstLine.IndexOf(time.ToString("HH:mm"), StringComparison.Ordinal);
            var remainingFirstLine = firstLine.Substring(timePrefixLength + 5).Trim();

            if (remainingFirstLine.Length > 0)
            {
                // Если есть текст после времени - это subject
                result.Subject = remainingFirstLine;
                
                // Остальные строки - это описание
                var descriptionLines = lines.Skip(1).ToList();
                if (descriptionLines.Count > 0)
                {
                    result.Description = string.Join(Environment.NewLine, descriptionLines);
                }
            }
            else if (lines.Length > 1)
            {
                // Если после времени пусто, но есть еще строки - это всё описание
                result.Subject = string.Join(Environment.NewLine, lines.Skip(1));
            }
        }
        else
        {
            // Нет времени - весь блок это subject (или subject + description)
            if (lines.Length == 1)
            {
                result.Subject = firstLine;
            }
            else
            {
                result.Subject = firstLine;
                result.Description = string.Join(Environment.NewLine, lines.Skip(1));
            }
        }

        return result;
    }

    private bool TryParseTime(string text, out TimeOnly time)
    {
        time = default;
        
        if (string.IsNullOrEmpty(text))
            return false;

        // Паттерн для времени в начале строки: "HH:mm" или "H:mm"
        if (text.Length >= 4 && text[0] >= '0' && text[0] <= '9')
        {
            var colonIndex = text.IndexOf(':');
            if (colonIndex > 0 && colonIndex <= 2)
            {
                var hourStr = text.Substring(0, colonIndex);
                var minStart = colonIndex + 1;
                if (minStart < text.Length && text[minStart] >= '0' && text[minStart] <= '9')
                {
                    var minEnd = Math.Min(minStart + 2, text.Length);
                    var minStr = text.Substring(minStart, minEnd - minStart);
                    
                    if (int.TryParse(hourStr, out var hour) && int.TryParse(minStr, out var minute))
                    {
                        if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
                        {
                            time = new TimeOnly(hour, minute);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private enum SectionType { Date, DifferentDates, Notes }
}
