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

    public async Task<ParsedFileData> ReadEventsAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                Log.Information($"📁 Файл событий не найден: {_filePath}");
                return new ParsedFileData { Events = [], ShoppingItems = [] };
            }

            var content = await File.ReadAllTextAsync(_filePath);
            var parseResult = _parser.ParseFile(content);
            
            var events = new List<EventData>();
            foreach (var section in parseResult.DateSections)
            {
                foreach (var parsedEvent in section.Events)
                {
                    events.Add(parsedEvent.Event);
                }
            }
            
            if (parseResult.DifferentDates != null)
            {
                foreach (var parsedEvent in parseResult.DifferentDates.Events)
                {
                    events.Add(parsedEvent.Event);
                }
            }
            
            var shoppingItems = new List<ShoppingItem>();
            if (parseResult.ShoppingSection != null)
            {
                shoppingItems.AddRange(parseResult.ShoppingSection.Items);
            }
            
            Log.Information($"📁 Прочитано событий из файла: {events.Count}, покупок: {shoppingItems.Count}");
            return new ParsedFileData { Events = events, ShoppingItems = shoppingItems };
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Ошибка чтения файла: {ex.Message}");
            return new ParsedFileData { Events = [], ShoppingItems = [] };
        }
    }
}