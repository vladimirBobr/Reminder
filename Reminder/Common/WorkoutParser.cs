using System.Text.RegularExpressions;

namespace ReminderApp.Common;

/// <summary>
/// Parser for coach running workouts from text messages
/// </summary>
public static class WorkoutParser
{
    /// <summary>
    /// Represents a parsed workout
    /// </summary>
    public class ParsedWorkout
    {
        public string DayName { get; set; } = "";
        public int DayNum { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
    }

    // Day of week mapping relative to Monday (0 = Monday, 1 = Tuesday, etc.)
    private static readonly Dictionary<string, int> DayOffset = new()
    {
        { "Пн", 0 }, { "Понедельник", 0 },
        { "Вт", 1 }, { "Вторник", 1 },
        { "Ср", 2 }, { "Среда", 2 },
        { "Чт", 3 }, { "Четверг", 3 },
        { "Пт", 4 }, { "Пятница", 4 },
        { "Сб", 5 }, { "Суббота", 5 },
        { "Вс", 6 }, { "Воскресенье", 6 }
    };

    private static readonly string[] DayKeywords = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

    /// <summary>
    /// Parse workout text and return list of workouts
    /// </summary>
    public static List<ParsedWorkout> Parse(string text, DateTime? referenceDate = null)
    {
        var workouts = new List<ParsedWorkout>();
        if (string.IsNullOrWhiteSpace(text))
            return workouts;

        // Get Monday of current week
        var today = referenceDate ?? DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var monday = today.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));

        // Find all day positions in text
        var dayPositions = new List<(string DayName, int Position)>();

        foreach (var dayKeyword in DayKeywords)
        {
            var regex = new Regex(dayKeyword, RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                dayPositions.Add((dayKeyword, match.Index));
            }
        }

        // Sort by position in text
        dayPositions.Sort((a, b) => a.Position.CompareTo(b.Position));

        // Extract description for each day
        for (int i = 0; i < dayPositions.Count; i++)
        {
            var current = dayPositions[i];
            var startPos = current.Position;
            var endPos = i < dayPositions.Count - 1 ? dayPositions[i + 1].Position : text.Length;

            var fullText = text.Substring(startPos, endPos - startPos);
            
            // Remove day name from description
            var description = fullText.Replace(current.DayName, "").Trim();

            // Step 1: Remove all emoji markers (✅, ✅️, ✓, etc.)
            description = Regex.Replace(description, @"✅️|✅|✓|🟢|●|➤|→", "");

            // Step 2: Split into lines, filter empty/whitespace-only, join back
            var lines = description.Split('\n');
            var cleanedLines = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            description = string.Join("\n", cleanedLines);

            if (description.Length > 2)
            {
                // Calculate date
                var dayOffset = DayOffset[current.DayName];
                var dayDate = monday.AddDays(dayOffset);

                workouts.Add(new ParsedWorkout
                {
                    DayName = current.DayName,
                    DayNum = dayOffset,
                    Date = dayDate,
                    Description = description
                });
            }
        }

        // Sort by date
        return workouts.OrderBy(w => w.Date).ToList();
    }
}