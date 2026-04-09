using ReminderApp.Common;

namespace ReminderApp.EventNotification;

/// <summary>
/// Реализация провайдера credentials для SmsAero
/// </summary>
public class SmsAeroCredentialsProvider : 
    EncryptedConfigCredentialsProvider<SmsAeroConfig, SmsAeroSettings>, 
    ISmsAeroCredentialsProvider
{
    public SmsAeroCredentialsProvider() 
        : base("smsaero-config.json", "SmsAeroConfig")
    {
    }

    protected override bool HasValidConfig(SmsAeroConfig config)
    {
        return !string.IsNullOrEmpty(config.Token);
    }

    protected override SmsAeroSettings ConvertToSettings(SmsAeroConfig config)
    {
        config.Token = UnprotectToken(config.Token);
        Console.WriteLine("✅ Загружены сохраненные SMSAero настройки");

        Console.Write("Введите номер телефона для SMS уведомлений (в формате 79000000000): ");
        var phoneNumber = Console.ReadLine()?.Trim();

        return new SmsAeroSettings(config.Email, config.Token, config.Sign, phoneNumber);
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
            SaveToSettings(new SmsAeroSettings(email, apiToken, sign, phoneNumber));
            Console.WriteLine("✅ SMSAero настройки сохранены");
        }

        return new SmsAeroSettings(email, apiToken, sign, phoneNumber);
    }

    protected override void SaveToSettings(SmsAeroSettings settings)
    {
        var config = new SmsAeroConfig
        {
            Email = settings.Email,
            Token = ProtectToken(settings.ApiToken),
            Sign = settings.Sign
        };

        SaveToFile(config);
    }
}

public class SmsAeroConfig
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Sign { get; set; } = "SMS Aero";
}