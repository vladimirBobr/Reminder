using System.Text.Json;

namespace ReminderApp.FileStorage;

public class JsonFileStorage : IFileStorage
{
    public async Task<Dictionary<string, DateTime>> LoadProcessedAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, DateTime>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? new Dictionary<string, DateTime>();
    }

    public async Task SaveProcessedAsync(string filePath, Dictionary<string, DateTime> processed)
    {
        var json = JsonSerializer.Serialize(processed, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<string?> LoadAsync(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllTextAsync(filePath);
    }

    public async Task SaveAsync(string key, string value)
    {
        var filePath = GetFilePath(key);
        await File.WriteAllTextAsync(filePath, value);
    }

    private string GetFilePath(string key)
    {
        // Sanitize key for file name
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return $"{safeKey}.json";
    }
}