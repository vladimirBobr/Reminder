using ReminderApp.EventReading;

namespace ReminderApp.EventReading;

/// <summary>
/// DTO для настроек GitHub
/// </summary>
public record GitHubSettings(
    string Url,
    string Token,
    string Owner,
    string Repo,
    string FilePath,
    string Branch
);

/// <summary>
/// Интерфейс для получения настроек GitHub
/// </summary>
public interface IGitHubCredentialsProvider
{
    /// <summary>
    /// Получить настройки GitHub (загрузить из файла или запросить через консоль)
    /// </summary>
    GitHubSettings GetCredentials();
}