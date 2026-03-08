using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace ReminderApp.EventReading;

public class EventReader : IEventReader
{
    private readonly Parser _parser = new();

    public async Task<List<EventData>> ReadEventsAsync()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var filePath = Path.Combine(userProfile, "events.txt");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ Файл {filePath} не найден. Создайте его вручную.");
            return new List<EventData>();
        }

        var content = await File.ReadAllTextAsync(filePath);
        return _parser.ParseEvents(content);
    }
}
