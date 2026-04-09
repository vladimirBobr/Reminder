using ReminderApp.Common;

namespace ReminderApp.EventNotification;

/// <summary>
/// Реализация провайдера credentials для SmsAero
/// Читает из зашифрованного файла, а если файла нет - запрашивает через консоль
/// </summary>
public class SmsAeroCredentialsProvider : EncryptedConfigCredentialsProvider<SmsAeroConfig>, ISmsAeroCredentialsProvider
{
    public SmsAeroCredentialsProvider()
        : base("smsaero-config.json", "SmsAeroConfig")
    {
    }

    public SmsAeroSettings GetCredentials()
    {
        var config = LoadFromFile();

        if (config != null && !string.IsNullOrEmpty(config.Token))
        {
            // Расшифровываем токен
            config.Token = UnprotectToken(config.Token);
            
            Console.WriteLine("✅ Загружены сохраненные SMSAero настройки");

            // Запрашиваем номер телефона если не сохранён
            Console.Write("Введите номер телефона для SMS уведомлений (в формате 79000000000): ");
            var phoneNumber = Console.ReadLine()?.Trim();

            return new SmsAeroSettings(config.Email, config.Token, config.Sign, phoneNumber);
        }

        return RequestCredentialsFromConsole();
    }

    private void SaveToFile(string email, string apiToken, string sign)
    {
        var config = new SmsAeroConfig
        {
            Email = email,
            Token = ProtectToken(apiToken),
            Sign = sign
        };

        SaveToFile(config);
    }

    private SmsAeroSettings RequestCredentialsFromConsole()
    {
        Console.WriteLine("Настройка SMSAero:");
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim() ?? "";

        Console.Write("API Token: ");
        var apiToken = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Подпись (по умолчанию SMS Aero): ");
        var sign = Console.ReadLine()?.Trim() ?? "SMS Aero";

        Console.Write("Номер телефона для уведомлений (в формате 79000000000): ");
        var phoneNumber = Console.ReadLine()?.Trim();

        // Сохраняем (кроме номера телефона)
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(apiToken))
        {
            SaveToFile(email, apiToken, sign);
            Console.WriteLine("✅ SMSAero настройки сохранены");
        }

        return new SmsAeroSettings(email, apiToken, sign, phoneNumber);
    }
}

public class SmsAeroConfig
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Sign { get; set; } = "SMS Aero";
}