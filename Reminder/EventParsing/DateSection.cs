namespace ReminderApp.EventParsing;

/// <summary>
/// Секция с конкретной датой. Содержит дату из заголовка и список событий.
/// </summary>
public class DateSection
{
    /// <summary>
    /// Дата из заголовка секции (например: 10.04.2026)
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Список распарсенных событий в этой секции.
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