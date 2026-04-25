using ReminderApp.Common;

namespace ReminderApp.FileStorage;

public static class FileStorageExtensions
{
    public static async Task<string?> LoadStringAsync(this IFileStorage fileStorage, string key)
    {
        try
        {
            return await fileStorage.LoadAsync(key);
        }
        catch
        {
            return null;
        }
    }

    public static async Task SaveStringAsync(this IFileStorage fileStorage, string key, string data)
    {
        try
        {
            await fileStorage.SaveAsync(key, data);
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Не удалось сохранить данные ({key}): {ex.Message}");
        }
    }
}