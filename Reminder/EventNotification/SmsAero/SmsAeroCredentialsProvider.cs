using ReminderApp.Common;

namespace ReminderApp.EventNotification.SmsAero;

/// <summary>
/// Реализация провайдера credentials для SmsAero
/// </summary>
public class SmsAeroCredentialsProvider : 
    EncryptedConfigCredentialsProvider<SmsAeroSettings>, 
    ISmsAeroCredentialsProvider
{
    public SmsAeroCredentialsProvider() 
        : base("smsaero-config.json", "SmsAeroConfig")
    {
    }

    protected override void DecryptSettings(SmsAeroSettings settings)
    {
        settings.ApiToken = UnprotectToken(settings.ApiToken);
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

        Console.WriteLine("✅ SMSAero настройки сохранены");

        return new SmsAeroSettings 
        { 
            Email = email, 
            ApiToken = apiToken, 
            Sign = sign, 
            PhoneNumber = phoneNumber 
        };
    }
}