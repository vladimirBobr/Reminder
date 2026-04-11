using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace Reminder.Tests.EventParsing;

// Тестовый файл для проверки парсинга времени
public class DebugTimeParsing
{
    [Fact]
    public void DebugSingleDigitHourParsing()
    {
        var parser = new FileParser();
        
        // Тест с однозначным часом
        var content = """
            # 10.04.2026 #
            9:30 Утреннее событие
            """;
        
        var events = parser.ParseEvents(content);
        
        // Выводим отладочную информацию
        Console.WriteLine($"Count: {events.Count}");
        foreach (var e in events)
        {
            Console.WriteLine($"Date: {e.Date}, Time: {e.Time}, Subject: '{e.Subject}'");
        }
        
        Assert.Single(events);
        Assert.Equal(new TimeOnly(9, 30), events[0].Time);
        Assert.Equal("Утреннее событие", events[0].Subject);
    }
    
    [Fact]
    public void DebugDoubleDigitHourParsing()
    {
        var parser = new FileParser();
        
        // Тест с двузначным часом
        var content = """
            # 10.04.2026 #
            18:30 Вечернее событие
            """;
        
        var events = parser.ParseEvents(content);
        
        // Выводим отладочную информацию
        Console.WriteLine($"Count: {events.Count}");
        foreach (var e in events)
        {
            Console.WriteLine($"Date: {e.Date}, Time: {e.Time}, Subject: '{e.Subject}'");
        }
        
        Assert.Single(events);
        Assert.Equal(new TimeOnly(18, 30), events[0].Time);
        Assert.Equal("Вечернее событие", events[0].Subject);
    }
}