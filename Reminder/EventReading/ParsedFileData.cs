using ReminderApp.Common;

namespace ReminderApp.EventReading;

/// <summary>
/// DTO containing all parsed data from the events file
/// </summary>
public class ParsedFileData
{
    /// <summary>Events with dates (regular calendar events)</summary>
    public required List<EventData> Events { get; init; }
    
    /// <summary>Items from #Shopping# section</summary>
    public required List<ShoppingItem> ShoppingItems { get; init; }
}