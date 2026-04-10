namespace ReminderApp.EventNotification.SmsRu;

/// <summary>
/// DTO для настроек SmsRu
/// </summary>
public record SmsRuSettings
{
    public string ApiId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Интерфейс для получения настроек SmsRu
/// </summary>
public interface ISmsRuCredentialsProvider
{
    /// <summary>
    /// Получить настройки SmsRu (загрузить из файла или запросить через консоль)
    /// </summary>
    SmsRuSettings GetCredentials();
}