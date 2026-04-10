using ReminderApp.Common;

namespace ReminderApp.EventNotification.Telegram;

/// <summary>
    /// Реализация провайдера credentials для Telegram
    /// </summary>
public class TelegramCredentialsProvider : 
    EncryptedConfigCredentialsProvider<TelegramSettings>, 
    ITelegramCredentialsProvider
{
    public TelegramCredentialsProvider() 
        : base("telegram-config.json", "TelegramConfig")
    {
    }

    protected override void DecryptSettings(TelegramSettings settings)
    {
        settings.BotToken = UnprotectToken(settings.BotToken);
    }

    protected override void EncryptSettings(TelegramSettings settings)
    {
        settings.BotToken = ProtectToken(settings.BotToken);
    }

    protected override TelegramSettings RequestFromConsole()
    {
        Console.WriteLine("Настройка Telegram:");
        Console.Write("Bot Token: ");
        var botToken = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Chat ID: ");
        var chatId = Console.ReadLine()?.Trim() ?? "";

        Console.WriteLine("✅ Telegram настройки сохранены");

        return new TelegramSettings 
        { 
            BotToken = botToken, 
            ChatId = chatId
        };
    }
}