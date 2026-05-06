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
        public string DayName { get; set; } = "";  // Full day name: "Вторник", "Четверг", etc.
        public int DayNum { get; set; }           // Offset from Monday (0 = Monday, 2 = Wednesday, etc.)
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
    }

    // Full day names mapping (short key -> full name, dayNum)
    private static readonly Dictionary<string, (string FullName, int DayNum)> DayMapping = new()
    {
        { "Пн", ("Понедельник", 0) },
        { "Вт", ("Вторник", 1) },
        { "Ср", ("Среда", 2) },
        { "Чт", ("Четверг", 3) },
        { "Пт", ("Пятница", 4) },
        { "Сб", ("Суббота", 5) },
        { "Вс", ("Воскресенье", 6) }
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

        // Step 1: Remove all ✅️ markers
        text = Regex.Replace(text, @"✅️|✅", "");

        // Get Monday of current week
        var today = referenceDate ?? DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        var monday = today.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));

        // Step 2: Find all day positions - day keyword at start of line, followed by space
        var dayPositions = new List<(string DayKeyword, int Position)>();

        foreach (var dayKeyword in DayKeywords)
        {
            var pattern = $@"{dayKeyword}\s+";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                var pos = match.Index;
                // Valid if: start of text OR after newline OR (after whitespace AND not preceded by letter)
                // This prevents matching "чт" in "можем что" but allows "Чт" after emoji removal
                bool valid = false;
                if (pos == 0)
                {
                    valid = true;
                }
                else if (text[pos - 1] == '\n' || text[pos - 1] == '\r')
                {
                    valid = true;
                }
                else if (char.IsWhiteSpace(text[pos - 1]))
                {
                    // After whitespace - valid only if NOT preceded by a letter
                    // This prevents "можем что" but allows "Эмодзи Чт"
                    if (pos < 2 || !char.IsLetter(text[pos - 2]))
                    {
                        valid = true;
                    }
                }
                
                if (valid)
                {
                    dayPositions.Add((dayKeyword, pos));
                }
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

            // Get text from start of day to start of next day (exclusive)
            var fullText = text.Substring(startPos, endPos - startPos);
            
            // Remove day keyword from beginning
            var description = fullText.TrimStart();
            if (description.StartsWith(current.DayKeyword, StringComparison.OrdinalIgnoreCase))
            {
                description = description.Substring(current.DayKeyword.Length).TrimStart();
            }

            // Clean up: split into lines, filter empty/whitespace-only, join back
            var lines = description.Split('\n');
            var cleanedLines = lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            description = string.Join("\n", cleanedLines);

            if (description.Length > 2)
            {
                // Calculate date
                var (fullName, dayNum) = DayMapping[current.DayKeyword];
                var dayDate = monday.AddDays(dayNum);

                workouts.Add(new ParsedWorkout
                {
                    DayName = fullName,
                    DayNum = dayNum,
                    Date = dayDate,
                    Description = description
                });
            }
        }

        // Sort by date
        return workouts.OrderBy(w => w.Date).ToList();
    }
}