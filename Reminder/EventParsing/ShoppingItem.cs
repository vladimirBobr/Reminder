namespace ReminderApp.EventParsing;

/// <summary>
/// Элемент списка покупок - простой текстовый блок.
/// </summary>
public class ShoppingItem
{
    /// <summary>
    /// Текст элемента списка покупок (может быть многострочным).
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Индекс первой строки элемента в исходном файле (0-based).
    /// </summary>
    public int StartLineIndex { get; init; }

    /// <summary>
    /// Индекс последней строки элемента в исходном файле (0-based).
    /// </summary>
    public int EndLineIndex { get; init; }
}