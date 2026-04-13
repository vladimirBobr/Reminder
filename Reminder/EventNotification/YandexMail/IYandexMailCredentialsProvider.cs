namespace ReminderApp.EventNotification.YandexMail;

/// <summary>
/// DTO for Yandex Mail settings
/// </summary>
public record YandexMailSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = "smtp.yandex.ru";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? ToEmail { get; set; }
}

/// <summary>
/// Interface for getting Yandex Mail settings
/// </summary>
public interface IYandexMailCredentialsProvider
{
    /// <summary>
    /// Get Yandex Mail settings (load from file or request via console)
    /// </summary>
    YandexMailSettings GetCredentials();
}