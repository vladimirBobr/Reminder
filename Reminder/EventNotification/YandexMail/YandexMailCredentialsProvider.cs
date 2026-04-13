using ReminderApp.Common;

namespace ReminderApp.EventNotification.YandexMail;

/// <summary>
/// Implementation of credentials provider for Yandex Mail
/// </summary>
public class YandexMailCredentialsProvider : 
    EncryptedConfigCredentialsProvider<YandexMailSettings>, 
    IYandexMailCredentialsProvider
{
    public YandexMailCredentialsProvider() 
        : base("yandexmail-config.json", "YandexMailConfig")
    {
    }

    protected override void DecryptSettings(YandexMailSettings settings)
    {
        settings.Password = UnprotectToken(settings.Password);
    }

    protected override void EncryptSettings(YandexMailSettings settings)
    {
        settings.Password = ProtectToken(settings.Password);
    }

    protected override YandexMailSettings RequestFromConsole()
    {
        Console.WriteLine("Настройка Yandex Mail:");
        Console.WriteLine("Для получения пароля приложения:");
        Console.WriteLine("1. Перейдите на https://id.yandex.ru/security");
        Console.WriteLine("2. Выберите 'Пароли приложений' -> 'Создать пароль'");
        Console.WriteLine("3. Назовите приложение (например, 'Reminder') и создайте пароль");
        Console.WriteLine();
        
        Console.Write("Email (полный адрес @yandex.ru): ");
        var email = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Пароль приложения: ");
        var password = Console.ReadLine()?.Trim() ?? "";

        Console.Write("SMTP хост (по умолчанию smtp.yandex.ru): ");
        var smtpHost = Console.ReadLine()?.Trim();
        
        Console.Write("SMTP порт (по умолчанию 587): ");
        var smtpPortStr = Console.ReadLine()?.Trim();
        
        Console.Write("Использовать SSL (по умолчанию да): ");
        var enableSslStr = Console.ReadLine()?.Trim().ToLower();
        
        Console.Write("Email получателя (куда отправлять уведомления): ");
        var toEmail = Console.ReadLine()?.Trim();

        Console.WriteLine("✅ Yandex Mail настройки сохранены");

        return new YandexMailSettings 
        { 
            Email = email, 
            Password = password,
            SmtpHost = string.IsNullOrEmpty(smtpHost) ? "smtp.yandex.ru" : smtpHost,
            SmtpPort = int.TryParse(smtpPortStr, out var port) ? port : 587,
            EnableSsl = string.IsNullOrEmpty(enableSslStr) || enableSslStr == "да" || enableSslStr == "yes" || enableSslStr == "y" || enableSslStr == "1" || enableSslStr == "true",
            ToEmail = string.IsNullOrEmpty(toEmail) ? null : toEmail
        };
    }
}