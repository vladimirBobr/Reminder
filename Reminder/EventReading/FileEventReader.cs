namespace ReminderApp.EventReading;

/// <summary>
/// Reads events from a local text file
/// </summary>
public class FileEventReader : EventReaderBase
{
    private readonly string _filePath;

    public FileEventReader(string filePath)
    {
        _filePath = filePath;
    }

    protected override async Task<string?> ReadContentAsync()
    {
        if (!File.Exists(_filePath))
        {
            Console.WriteLine($"❌ File {_filePath} not found.");
            return null;
        }

        return await File.ReadAllTextAsync(_filePath);
    }
}