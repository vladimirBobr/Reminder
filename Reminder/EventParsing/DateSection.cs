namespace ReminderApp.EventParsing;

/// <summary>
/// Секция с конкретной датой. Содержит дату из заголовка и список текстовых блоков (событий).
/// </summary>
public class DateSection
{
    /// <summary>
    /// Дата из заголовка секции (например: 10.04.2026)
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Список текстовых блоков (событий) в этой секции. 
    /// Каждый блок - это многострочный текст события.
    /// </summary>
    public required List<string> EventBlocks { get; init; }
}