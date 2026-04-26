using System.Net.Http.Headers;
using System.Text;
using ReminderApp.EventNotification;

namespace ReminderApp.EventNotification.Ntfy;

public class NtfyNotifier : INtfyNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _topic;

    public NtfyNotifier(INtfyCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _serverUrl = settings.ServerUrl.TrimEnd('/');
        _topic = settings.Topic;

        var handler = new HttpClientHandler();
        var proxy = ProxyHelper.CreateProxy();
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        _httpClient = new HttpClient(handler);
        
        // Set up basic authentication if credentials are provided
        if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task NotifyAsync(string message)
    {
        if (DebugHelper.IsDebug)
        {
            Log.Information($"[NTFY DEBUG] Topic: /{_topic}");
            Log.Information($"[NTFY DEBUG] {message}");
            return;
        }

        var url = $"{_serverUrl}/{_topic}";

        try
        {
            var content = new StringContent(message, Encoding.UTF8, "text/plain");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                Log.Information($"❌ Ntfy API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
            else
            {
                Log.Information("✅ Уведомление отправлено в Ntfy");
            }
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Ошибка отправки в Ntfy: {ex.Message}");
        }
    }
}
