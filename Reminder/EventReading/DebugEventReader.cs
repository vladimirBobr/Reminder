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
            
            // Длинное описание для тестирования UI
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(2)),
                Time = new TimeOnly(15, 30),
                Subject = "Очень длинное название мероприятия которое должно переноситься на несколько строк для проверки"
            },
            
            // Ещё одно длинное
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(3)),
                Subject = "Краткое"
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(3)),
                Subject = "Среднее название мероприятия для проверки отображения"
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(3)),
                Subject = "Ещё одно мероприятие с достаточно длинным названием чтобы проверить перенос текста на новую строку и адаптивность"
            },
            
            // Выходные
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(4)), // суббота
                Time = new TimeOnly(10, 0),
                Subject = "Субботний пикник с очень длинным описанием мероприятия которое должно показываться корректно"
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(5)), // воскресенье
                Time = new TimeOnly(12, 0),
                Subject = "Воскресный семейный завтрак"
            },
            
            // Тест длинного Description
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(6)),
                Time = new TimeOnly(14, 0),
                Subject = "Совещание по проекту",
                Description = "Очень длинное описание совещания которое должно показываться под основным названием мероприятия и занимать несколько строк при отображении в интерфейсе админки"
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(6)),
                Subject = "Краткое событие",
                Description = "Короткое описание"
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(7)),
                Time = new TimeOnly(10, 30),
                Subject = "Презентация нового продукта",
                Description = "Презентация для команды и стейкхолдеров. Ожидаем много участников. Подготовить демо и слайды."
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddDays(2)),
                Subject = "Задание на среду",
                Description = "- Длительный бег 20 км\n- не забыть с собой в машину воду\n- взять влажную салфетку и гель\n- забежать на родник"
            },
        };

        return Task.FromResult(new ParsedFileData { Events = events, ShoppingItems = [] });
    }
}
