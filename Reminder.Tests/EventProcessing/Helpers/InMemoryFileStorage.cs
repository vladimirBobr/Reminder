using ReminderApp.FileStorage;

namespace Reminder.Tests.EventProcessing.Helpers;

public class InMemoryFileStorage : IFileStorage
{
    private Dictionary<string, DateTime> _processed = new();

    public Task<Dictionary<string, DateTime>> LoadProcessedAsync(string filePath)
    {
        return Task.FromResult(_processed);
    }

    public Task SaveProcessedAsync(string filePath, Dictionary<string, DateTime> processed)
    {
        _processed = processed;
        return Task.CompletedTask;
    }

    public void SetProcessed(Dictionary<string, DateTime> processed)
    {
        _processed = processed;
    }

    public Dictionary<string, DateTime> GetProcessed()
    {
        return _processed;
    }
}
