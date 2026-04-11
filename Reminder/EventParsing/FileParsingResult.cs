namespace ReminderApp.EventParsing;

/// <summary>
/// Результат верхнеуровневого парсинга файла событий.
/// </summary>
public class FileParsingResult
{
    /// <summary>
    /// Секции с конкретными датами (в порядке появления в файле).
    /// </summary>
    public required List<DateSection> DateSections { get; init; }

    /// <summary>
    /// Секция с разными датами (DifferentDates). Может быть null если секция отсутствует.
    /// </summary>
    public DifferentDatesSection? DifferentDates { get; init; }

    /// <summary>
    /// Секция заметок (Notes). Игнорируется при парсинге событий.
    /// </summary>
    public NotesSection? NotesSection { get; init; }
}