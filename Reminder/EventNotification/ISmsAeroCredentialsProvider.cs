namespace ReminderApp.EventNotification;

/// <summary>
/// DTO для настроек SmsAero
/// </summary>
public record SmsAeroSettings(
    string Email,
    string ApiToken,
    string Sign,
    string? PhoneNumber
);

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