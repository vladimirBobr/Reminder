using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReminderApp.EventNotification.SmsRu;

public class SmsRuNotifier : INotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _apiId;
    private const string BASE_URL = "https://sms.ru/sms/send";

    public SmsRuNotifier(ISmsRuCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _apiId = settings.ApiId;

        // Сохраняем номер телефона для использования в уведомлениях
        if (!string.IsNullOrEmpty(settings.PhoneNumber))
        {
            Environment.SetEnvironmentVariable("SMSRU_DEFAULT_PHONE", settings.PhoneNumber);
        }

        var handler = new HttpClientHandler();
        var proxy = ProxyHelper.CreateProxy();
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        _httpClient = new HttpClient(handler);
    }

    public void Notify(string message)
    {
        var phoneNumber = GetDefaultPhoneNumber();
        
        if (string.IsNullOrEmpty(phoneNumber))
        {
            Log.Information("❌ SMS.RU: Phone number not specified");
            return;
        }

        try
        {
            var response = SendSms(phoneNumber, message);
            
            if (response.Status == "OK")
            {
                Log.Information($"✅ SMS отправлен через SMS.RU на {phoneNumber}");
            }
            else
            {
                Log.Information($"❌ SMS.RU API error: {response.StatusText}");
            }
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Ошибка отправки SMS: {ex.Message}");
        }
    }

    private SmsRuResponse SendSms(string number, string text)
    {
        var url = $"{BASE_URL}?api_id={_apiId}&to={number}&msg={Uri.EscapeDataString(text)}&json=1";
        
        var response = _httpClient.GetAsync(url).Result;
        
        var content = response.Content.ReadAsStringAsync().Result;
        
        try
        {
            return JsonSerializer.Deserialize<SmsRuResponse>(content) ?? new SmsRuResponse { Status = "ERROR", StatusText = "Failed to deserialize response" };
        }
        catch
        {
            return new SmsRuResponse { Status = "ERROR", StatusText = content };
        }
    }

    private string? GetDefaultPhoneNumber()
    {
        return Environment.GetEnvironmentVariable("SMSRU_DEFAULT_PHONE");
    }
}

public class SmsRuResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("status_text")]
    public string? StatusText { get; set; }
    
    [JsonPropertyName("sms")]
    public Dictionary<string, SmsRuSmsData>? Sms { get; set; }
}

public class SmsRuSmsData
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("cost")]
    public string? Cost { get; set; }
    
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
}