using ReminderApp.Common;

namespace ReminderApp.EventParsing;

internal class Parser
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
            throw new Exception();

        var lines = block.Split([Environment.NewLine], StringSplitOptions.None);

        var firstLine = lines.First(); // точно должна быть
        string description = string.Join(Environment.NewLine, lines.Skip(1));

        (DateOnly date, TimeOnly? time, string? subject)? parsed = ParseFirstLine(firstLine);
        if (parsed == null)
            return CreateErrorEvent(block);

        var date = parsed.Value.date;
        var time = parsed.Value.time;
        var subject = parsed.Value.subject;

        // Шаг 3: Формируем EventData
        
        var eventData = new EventData
        {
            Time = date.ToDateTime(time ?? TimeOnly.MinValue),
            Subject = subject,
            Description = description
        };

        return eventData;
    }

    internal (DateOnly date, TimeOnly? time, string? subject)? ParseFirstLine(string firstLine)
    {
        if (string.IsNullOrWhiteSpace(firstLine))
            throw new Exception();

        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return null;

        DateOnly date;
        TimeOnly? time = null;
        string? subject;

        // Проверяем, является ли первый элемент датой
        if (DateOnly.TryParseExact(parts[0],
                "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateOnly parsedDate))
        {
            // Первый элемент - дата
            date = parsedDate;

            // Проверяем второй элемент на время
            if (parts.Length >= 2)
            {
                if (TimeOnly.TryParse(parts[1], out TimeOnly parsedTime))
                {
                    time = parsedTime;
                    subject = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : null;
                }
                else
                {
                    subject = string.Join(" ", parts.Skip(1));
                }
            }
            else
            {
                subject = null;
            }
        }
        else if (TimeOnly.TryParse(parts[0], out TimeOnly parsedTime))
        {
            // Первый элемент - время (используем сегодняшнюю дату)
            date = DateOnly.FromDateTime(DateTime.Today);
            time = parsedTime;
            subject = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null;
        }
        else
        {
            // Первый элемент - не дата и не время (невалидная строка)
            return null;
        }

        return (date, time, subject);
    }



    private EventData CreateErrorEvent(string block)
    {
        return new EventData
        {
            Time = DateTime.Today,
            Subject = Parser.ErrorSubject, // "[parsing error]"
            Description = block
        };
    }
}
