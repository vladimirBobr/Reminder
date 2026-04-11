namespace ReminderApp.EventParsing;

/// <summary>
/// Секция заметок - игнорируется при парсинге событий.
/// </summary>
public class NotesSection
{
    /// <summary>
    /// Список текстовых блоков в этой секции.
    /// </summary>
    public required List<string> EventBlocks { get; init; }
}