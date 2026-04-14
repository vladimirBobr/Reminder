using System.Net.Http.Headers;
using System.Text;

namespace ReminderApp.EventNotification.Ntfy;

public class NtfyNotifier : INotifier
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
        var url = $"{_serverUrl}/{_topic}/json";

        try
        {
            var content = new StringContent($"\"{EscapeJson(message)}\"", Encoding.UTF8, "application/json");
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

    private static string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
