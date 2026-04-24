namespace ReminderApp.EventParsing;

/// <summary>
/// Секция заметок - игнорируется при парсинге событий.
/// </summary>
public class NotesSection
{
    /// <summary>
    /// Список текстовых блоков в этой секции.
    /// </summary>
    public required List<ParsedEvent> Events { get; init; }

    /// <summary>
    /// Индекс строки заголовка секции в исходном файле (0-based).
    /// </summary>
    public int HeaderLineIndex { get; init; }

    /// <summary>
    /// Индекс первой строки контента (после заголовка) в исходном файле (0-based).
    /// </summary>
    public int ContentStartLineIndex { get; init; }

    /// <summary>
    /// Индекс последней строки контента (до следующего заголовка или конца) в исходном файле (0-based).
    /// </summary>
    public int ContentEndLineIndex { get; init; }
}