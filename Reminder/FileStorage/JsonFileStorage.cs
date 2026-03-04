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
}
