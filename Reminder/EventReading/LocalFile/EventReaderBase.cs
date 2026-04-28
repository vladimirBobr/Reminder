using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace ReminderApp.EventReading.LocalFile;

/// <summary>
/// Base class for event readers that provides common parsing functionality
/// </summary>
public abstract class EventReaderBase : IEventReader
{
    protected readonly FileParser Parser = new();

    public async Task<ParsedFileData> ReadEventsAsync()
    {
        var content = await ReadContentAsync();
        
        if (string.IsNullOrEmpty(content))
        {
            return new ParsedFileData { Events = [], ShoppingItems = [] };
        }
        
        var parseResult = Parser.ParseFile(content);
        
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
        
        return new ParsedFileData 
        { 
            Events = events,
            ShoppingItems = shoppingItems
        };
    }

    /// <summary>
    /// Reads raw content from the source
    /// </summary>
    protected abstract Task<string?> ReadContentAsync();
}