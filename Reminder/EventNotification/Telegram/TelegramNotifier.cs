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

    public void Notify(string message)
    {
        var url = $"sendMessage?chat_id={_chatId}&text={Uri.EscapeDataString(message)}&parse_mode=Markdown";

        try
        {
            var response = _httpClient.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                Log.Information($"❌ Telegram API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
            else
            {
                Log.Information("✅ Уведомление отправлено в Telegram");
            }
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Ошибка отправки в Telegram: {ex.Message}");
        }
    }
}