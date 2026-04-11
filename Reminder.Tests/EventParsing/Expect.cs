using ReminderApp.Common;

namespace Reminder.Tests.EventParsing;

// =============== Fluent API для построения ожидаемого результата ===============

/// <summary>
/// Билдер для создания ожидаемой секции с датой.
/// Пример: Expect.DateSection("10.04.2026").WithBlocks("событие 1", "событие 2")
/// </summary>
public static class Expect
{
    /// <summary>Создаёт ожидаемую секцию с датой</summary>
    public static DateSectionBuilder DateSection(string date)
    {
        return new DateSectionBuilder(date);
    }

    /// <summary>Создаёт ожидаемую секцию DifferentDates</summary>
    public static DifferentDatesBuilder DifferentDates()
    {
        return new DifferentDatesBuilder();
    }

    /// <summary>Создаёт ожидаемую секцию Notes</summary>
    public static NotesBuilder Notes()
    {
        return new NotesBuilder();
    }

    /// <summary>Создаёт ожидаемое событие EventData</summary>
    public static EventDataBuilder Event(string? subject = null)
    {
        return new EventDataBuilder { Subject = subject };
    }
}
