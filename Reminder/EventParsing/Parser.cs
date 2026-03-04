using ReminderApp.Events;

namespace ReminderApp.EventParsing;

internal class Parser
{
    public List<EventData> ParseEvents(string content)
    {
        var events = new List<EventData>();
        var lines = content.Split('\n').Select(l => l.Trim()).ToList();
        var currentEvent = new EventData();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                if (!string.IsNullOrEmpty(currentEvent.Subject))
                {
                    events.Add(currentEvent);
                    currentEvent = new EventData();
                }
                continue;
            }

            if (string.IsNullOrEmpty(currentEvent.Subject))
            {
                // Парсим первую строку: дата [время] текст
                var parts = line.Split(' ', 3);
                if (parts.Length < 2) continue;

                string dateStr = parts[0];
                string timeStr = parts.Length > 2 ? parts[1] : "00:00";
                string subject = parts.Length > 2 ? parts[2] : parts[1];

                // Парсим дату
                var dateParts = dateStr.Split('.');
                int day = ParsePart(dateParts[0], DateTime.Now.Day);
                int month = ParsePart(dateParts[1], DateTime.Now.Month);
                int year = ParsePart(dateParts[2], DateTime.Now.Year);

                int hour = int.Parse(timeStr.Split(':')[0]);
                int minute = int.Parse(timeStr.Split(':')[1]);

                var eventTime = new DateTime(year, month, day, hour, minute, 0);

                currentEvent.Time = eventTime;
                currentEvent.Subject = subject;
                currentEvent.Description = subject; // Пока так, потом добавим многострочность
            }
            else
            {
                // Многострочность: добавляем к описанию
                currentEvent.Description += "\n" + line;
            }
        }

        if (!string.IsNullOrEmpty(currentEvent.Subject))
            events.Add(currentEvent);

        return events;
    }

    private int ParsePart(string part, int defaultValue)
    {
        return part == "*" ? defaultValue : int.Parse(part);
    }
}
