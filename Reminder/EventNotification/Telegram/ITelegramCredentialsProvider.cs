namespace ReminderApp.EventNotification.Telegram;

/// <summary>
/// DTO для настроек Telegram
/// </summary>
public record TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

/// <summary>
/// Интерфейс для получения настроек Telegram
/// </summary>
public interface ITelegramCredentialsProvider
{
    /// <summary>
    /// Получить настройки Telegram (загрузить из файла или запросить через консоль)
    /// </summary>
    TelegramSettings GetCredentials();
}