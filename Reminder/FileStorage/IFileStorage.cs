namespace ReminderApp.FileStorage;

public interface IFileStorage
{
    // Существующие методы для processed events
    Task<Dictionary<string, DateTime>> LoadProcessedAsync(string filePath);
    Task SaveProcessedAsync(string filePath, Dictionary<string, DateTime> processed);

    // Универсальные методы для произвольных данных
    Task<string?> LoadAsync(string key);
    Task SaveAsync(string key, string value);
}