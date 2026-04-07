using ReminderApp.Common;

namespace ReminderApp.EventParsing;

public class Parser
{
    private const string ActualLineSeparator = "actual line:===============================================================";
    internal const string ErrorSubject = "[parsing error]";

    public List<EventData> ParseEvents(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        // Шаг 1: Разделяем на актуальную и неактуальную части
        var parts = content.Split([ActualLineSeparator], StringSplitOptions.None);
        var actualContent = parts.FirstOrDefault() ?? content; // Если разделитель не найден — берем весь контент

        // Шаг 2: Разделяем на куски по пустым строкам
        var rawEventBlocks = TextBlockSplitter.SplitIntoBlocks(actualContent);

        // Шаг 3: Преобразуем каждый кусок в EventData
        var events = new List<EventData>();
        foreach (var block in rawEventBlocks)
        {
            events.Add(ParseEventBlock(block));
        }

        return events;
    }

    internal EventData ParseEventBlock(string block)
    {
        if (string.IsNullOrEmpty(block))
            throw new Exception("Empty event block");

        var lines = block.Split([Environment.NewLine], StringSplitOptions.None);

        var firstLine = lines.First(); // точно должна быть
        string description = string.Join(Environment.NewLine, lines.Skip(1));

        var (date, time, subject) = ParseFirstLine(firstLine);

        return new EventData
        {
            Date = date,
            Time = time,
            Subject = subject,
            Description = description
        };
    }

    private (DateOnly? Date, TimeOnly? Time, string? Subject)
       ParseFirstLine(string firstLine)
    {
        if (string.IsNullOrWhiteSpace(firstLine))
            throw new Exception("First line is empty");

        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Пробуем распарсить первый элемент как дату
        if (DateOnly.TryParseExact(parts[0], "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var dateOnly))
        {
            // Есть дата
            TimeOnly? time = null;
            string? subject = null;

            // Проверяем второй элемент на время
            if (parts.Length >= 2 && TimeOnly.TryParse(parts[1], out var parsedTime))
            {
                time = parsedTime;
                // Остальное — subject (если есть)
                subject = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : null;
            }
            else if (parts.Length >= 2)
            {
                // Второй элемент — не время, значит это начало subject
                subject = string.Join(" ", parts.Skip(1));
            }
            // else: только дата, без времени и subject

            return (dateOnly, time, subject);
        }

        // Пробуем распарсить первый элемент как время
        if (TimeOnly.TryParse(parts[0], out var timeOnly))
        {
            // Есть время
            string? subject = null;

            // Остальное — subject (если есть)
            if (parts.Length > 1)
            {
                subject = string.Join(" ", parts.Skip(1));
            }

            return (null, timeOnly, subject);
        }

        // Нет ни даты, ни времени — вся строка это subject
        return (null, null, string.Join(" ", parts));
    }
}
