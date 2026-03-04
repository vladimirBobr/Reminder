namespace ReminderApp.FileStorage;

public interface IFileStorage
{
    Task<Dictionary<string, DateTime>> LoadProcessedAsync(string filePath);
    Task SaveProcessedAsync(string filePath, Dictionary<string, DateTime> processed);
}
