using ReminderApp.Common;

namespace ReminderApp.EventNotification;

/// <summary>
/// Реализация провайдера credentials для SmsAero
/// </summary>
public class SmsAeroCredentialsProvider : 
    EncryptedConfigCredentialsProvider<SmsAeroSettings>, 
    ISmsAeroCredentialsProvider
{
    // DTO для хранения в файле (с зашифрованным токеном)
    private class SmsAeroStoredSettings
    {
        public string Email { get; set; } = string.Empty;
        public string EncryptedToken { get; set; } = string.Empty;
        public string Sign { get; set; } = "SMS Aero";
    }

    public SmsAeroCredentialsProvider() 
        : base("smsaero-config.json", "SmsAeroConfig")
    {
    }

    protected override bool HasValidSettings(SmsAeroSettings settings)
    {
        return !string.IsNullOrEmpty(settings.ApiToken);
    }

    protected override void DecryptSettings(SmsAeroSettings settings)
    {
        // Token уже дешифрован при загрузке
    }

    protected override void EncryptSettings(SmsAeroSettings settings)
    {
        settings.ApiToken = ProtectToken(settings.ApiToken);
    }

    protected override SmsAeroSettings RequestFromConsole()
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

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(apiToken))
        {
            Console.WriteLine("✅ SMSAero настройки сохранены");
        }

        return new SmsAeroSettings { Email = email, ApiToken = apiToken, Sign = sign, PhoneNumber = phoneNumber };
    }

    protected override sealed SmsAeroSettings? LoadFromFile()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            var stored = System.Text.Json.JsonSerializer.Deserialize<SmsAeroStoredSettings>(json);
            
            if (stored == null)
                return null;

            Console.WriteLine("✅ Загружены сохраненные SMSAero настройки");
            
            Console.Write("Введите номер телефона для SMS уведомлений (в формате 79000000000): ");
            var phoneNumber = Console.ReadLine()?.Trim();

            return new SmsAeroSettings
            {
                Email = stored.Email,
                ApiToken = UnprotectToken(stored.EncryptedToken),
                Sign = stored.Sign,
                PhoneNumber = phoneNumber
            };
        }
        catch
        {
            return null;
        }
    }
}