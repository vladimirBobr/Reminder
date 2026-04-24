using ReminderApp.Common;

namespace ReminderApp.EventParsing;

/// <summary>
/// Обёртка над EventData с информацией о границах строк в исходном файле.
/// </summary>
public class ParsedEvent
{
    /// <summary>
    /// Распарсенное событие.
    /// </summary>
    public required EventData Event { get; init; }

    /// <summary>
    /// Индекс первой строки события в исходном файле (0-based).
    /// </summary>
    public int StartLineIndex { get; init; }

    /// <summary>
    /// Индекс последней строки события в исходном файле (0-based).
    /// </summary>
    public int EndLineIndex { get; init; }
}