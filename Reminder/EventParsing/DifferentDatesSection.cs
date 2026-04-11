namespace ReminderApp.EventParsing;

/// <summary>
/// Секция "DifferentDates" - содержит события с произвольными датами.
/// </summary>
public class DifferentDatesSection
{
    /// <summary>
    /// Список текстовых блоков (событий) в этой секции.
    /// Каждый блок - это многострочный текст события.
    /// </summary>
    public required List<string> EventBlocks { get; init; }
}