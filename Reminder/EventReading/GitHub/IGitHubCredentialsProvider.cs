namespace ReminderApp.EventReading.GitHub;

/// <summary>
/// DTO для настроек GitHub
/// </summary>
public record GitHubSettings
{
    public string Url { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

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