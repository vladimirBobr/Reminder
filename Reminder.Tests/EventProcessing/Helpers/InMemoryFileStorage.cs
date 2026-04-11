using ReminderApp.FileStorage;

namespace Reminder.Tests.EventProcessing.Helpers;

public class InMemoryFileStorage : IFileStorage
{
    private Dictionary<string, DateTime> _processed = new();
    private Dictionary<string, string> _data = new();

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

    public Task<string?> LoadAsync(string key)
    {
        return Task.FromResult(_data.GetValueOrDefault(key));
    }

    public Task SaveAsync(string key, string value)
    {
        _data[key] = value;
        return Task.CompletedTask;
    }
}