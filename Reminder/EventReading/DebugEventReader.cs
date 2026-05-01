using ReminderApp.Common;

namespace ReminderApp.EventReading;

public class DebugEventReader : IEventReader
{
    private readonly DateTime _frozenNow;

    public DebugEventReader()
    {
        _frozenNow = DateTime.Now;
    }

    public Task<ParsedFileData> ReadEventsAsync()
    {
        var now = _frozenNow;
        var events = new List<EventData>
        {
            // Событие через 30 минут - для тестирования ReminderProcessor
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddMinutes(30)), 
                Time = TimeOnly.FromDateTime(now.AddMinutes(30)), 
                Subject = "Событие через 30 минут", 
                Description = "Должно прийти как напоминание" 
            },
            
            // Событие через 2.5 часа - для ежедневного дайджеста
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddHours(2).AddMinutes(30)), 
                Time = TimeOnly.FromDateTime(now.AddHours(2).AddMinutes(30)), 
                Subject = "Событие через 2.5 часа", 
                Description = "В дайджесте" 
            },
            
            // Завтра
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddDays(1)), 
                Time = new TimeOnly(10, 0), 
                Subject = "Планирование спринта" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddDays(1)), 
                Subject = "Обзор дизайна" 
            },
            
            // Следующая неделя (пн)
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddDays(7)), 
                Time = new TimeOnly(9, 0), 
                Subject = "Старт нового спринта" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddDays(7)), 
                Time = new TimeOnly(14, 0), 
                Subject = "Демо для стейкхолдеров" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(now.AddDays(8)), 
                Time = new TimeOnly(11, 0), 
                Subject = "Технический созвон" 
            },
        };

        return Task.FromResult(new ParsedFileData { Events = events, ShoppingItems = [] });
    }
}
