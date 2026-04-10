using System.Text;
using ReminderApp.Common;

namespace ReminderApp.EventNotification.Telegram;

public class TelegramNotifier : INotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private readonly string _chatId;

    public TelegramNotifier(ITelegramCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _botToken = settings.BotToken;
        _chatId = settings.ChatId;

        var handler = new HttpClientHandler();
        var proxy = ProxyHelper.CreateProxy();
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{_botToken}/");
    }

    public void Notify(EventData eventData)
    {
        var message = FormatMessage(eventData);
        var url = $"sendMessage?chat_id={_chatId}&text={Uri.EscapeDataString(message)}&parse_mode=Markdown";

        try
        {
            var response = _httpClient.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Telegram API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
            else
            {
                Console.WriteLine($"✅ Уведомление отправлено в Telegram: {eventData.Subject}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка отправки в Telegram: {ex.Message}");
        }
    }

    private string FormatMessage(EventData eventData)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"📅 *{eventData.Time:dd.MM.yyyy HH:mm}*");
        if (!string.IsNullOrEmpty(eventData.Subject))
            sb.AppendLine($"📌 *{eventData.Subject}*");
        if (!string.IsNullOrEmpty(eventData.Description))
            sb.AppendLine($"📝 {eventData.Description}");

        return sb.ToString();
    }
}