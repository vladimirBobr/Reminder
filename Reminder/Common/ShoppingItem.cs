namespace ReminderApp.Common;

/// <summary>
/// Элемент списка покупок - простой текстовый блок.
/// </summary>
public class ShoppingItem
{
    /// <summary>
    /// Текст элемента списка покупок (может быть многострочным).
    /// </summary>
    public required string Subject { get; init; }
}
