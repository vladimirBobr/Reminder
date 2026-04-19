using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace ReminderApp.EventReading.LocalFile;

public class FileEventReader : IEventReader
{
    private readonly string _filePath;
    private readonly FileParser _parser;

    public FileEventReader(string filePath)
    {
        _filePath = filePath;
        _parser = new FileParser();
    }

    public async Task<List<EventData>> ReadEventsAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                Log.Information($"📁 Файл событий не найден: {_filePath}");
                return new List<EventData>();
            }

            var content = await File.ReadAllTextAsync(_filePath);
            var events = _parser.ParseEvents(content);
            
            Log.Information($"📁 Прочитано событий из файла: {events.Count}");
            return events;
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Ошибка чтения файла: {ex.Message}");
            return new List<EventData>();
        }
    }
}