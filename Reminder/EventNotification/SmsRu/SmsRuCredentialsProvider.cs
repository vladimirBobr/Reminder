using ReminderApp.Common;

namespace ReminderApp.EventNotification.SmsRu;

/// <summary>
/// Реализация провайдера credentials для SmsRu
/// </summary>
public class SmsRuCredentialsProvider : 
    EncryptedConfigCredentialsProvider<SmsRuSettings>, 
    ISmsRuCredentialsProvider
{
    public SmsRuCredentialsProvider() 
        : base("smsru-config.json", "SmsRuConfig")
    {
    }

    protected override void DecryptSettings(SmsRuSettings settings)
    {
        settings.ApiId = UnprotectToken(settings.ApiId);
    }

    protected override void EncryptSettings(SmsRuSettings settings)
    {
        settings.ApiId = ProtectToken(settings.ApiId);
    }

    protected override SmsRuSettings RequestFromConsole()
    {
        Log.Information("Настройка SMS.RU:");
        Console.Write("API ID: ");
        var apiId = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Номер телефона для уведомлений (в формате 79000000000): ");
        var phoneNumber = Console.ReadLine()?.Trim();

        Log.Information("✅ SMS.RU настройки сохранены");

        return new SmsRuSettings 
        { 
            ApiId = apiId, 
            PhoneNumber = phoneNumber 
        };
    }
}