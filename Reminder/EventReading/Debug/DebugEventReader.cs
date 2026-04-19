using ReminderApp.Common;

namespace ReminderApp.EventReading.Debug;

public class DebugEventReader : IEventReader
{
    public Task<List<EventData>> ReadEventsAsync()
    {
        var events = new List<EventData>
        {
            // Сегодня
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(0)), 
                Time = new TimeOnly(15, 0), 
                Subject = "Совещание с командой", 
                Description = "Ежедневный стендап" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(0)), 
                Time = new TimeOnly(16, 30), 
                Subject = "Встреча с клиентом" 
            },
            
            // Завтра
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), 
                Time = new TimeOnly(10, 0), 
                Subject = "Планирование спринта" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), 
                Subject = "Обзор дизайна" 
            },
            
            // Следующая неделя (пн)
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), 
                Time = new TimeOnly(9, 0), 
                Subject = "Старт нового спринта" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), 
                Time = new TimeOnly(14, 0), 
                Subject = "Демо для стейкхолдеров" 
            },
            new() 
            { 
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(8)), 
                Time = new TimeOnly(11, 0), 
                Subject = "Технический созвон" 
            },
        };

        return Task.FromResult(events);
    }
}