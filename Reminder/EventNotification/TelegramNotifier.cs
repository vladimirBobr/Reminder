using System.Net;
using System.Text;
using ReminderApp.Common;

namespace ReminderApp.EventNotification;

public class TelegramNotifier : INotifier
{
    private readonly HttpClient _httpClient;
    private const string BOT_TOKEN = "";
    private const string CHAT_ID = ""; // 

    public TelegramNotifier()
    {
        var handler = new HttpClientHandler
        {
            Proxy = ProxyHelper.CreateProxy(),
            UseProxy = true
        };

        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri($"https://api.telegram.org/bot{BOT_TOKEN}/");
    }

    public void Notify(EventData eventData)
    {
        var message = FormatMessage(eventData);
        var url = $"sendMessage?chat_id={CHAT_ID}&text={Uri.EscapeDataString(message)}&parse_mode=Markdown";

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

public static class ProxyHelper
{
    public static void ConfigProxy()
    {
        WebRequest.DefaultWebProxy = CreateProxy();
    }

    public static WebProxy CreateProxy()
    {
#if DEBUG
        var webProxy = new WebProxy("", 9090);
#else
        
#endif

        webProxy.UseDefaultCredentials = true;
        return webProxy;
    }
}
