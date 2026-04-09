namespace ReminderApp.EventNotification.SmsAero;

/// <summary>
/// DTO для настроек SmsAero
/// </summary>
public record SmsAeroSettings
{
    public string Email { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string Sign { get; set; } = "SMS Aero";
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Интерфейс для получения настроек SmsAero
/// </summary>
public interface ISmsAeroCredentialsProvider
{
    /// <summary>
    /// Получить настройки SmsAero (загрузить из файла или запросить через консоль)
    /// </summary>
    SmsAeroSettings GetCredentials();
}