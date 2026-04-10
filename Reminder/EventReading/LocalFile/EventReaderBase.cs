using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace ReminderApp.EventReading.LocalFile;

/// <summary>
/// Base class for event readers that provides common parsing functionality
/// </summary>
public abstract class EventReaderBase : IEventReader
{
    protected readonly Parser Parser = new();

    public async Task<List<EventData>> ReadEventsAsync()
    {
        var content = await ReadContentAsync();
        
        if (string.IsNullOrEmpty(content))
        {
            return new List<EventData>();
        }
        
        return Parser.ParseEvents(content);
    }

    /// <summary>
    /// Reads raw content from the source
    /// </summary>
    protected abstract Task<string?> ReadContentAsync();
}