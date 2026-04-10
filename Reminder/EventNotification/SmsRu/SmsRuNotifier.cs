using System.Text;
using System.Text.Json;
using ReminderApp.Common;

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

    public void Notify(EventData eventData)
    {
        var message = FormatMessage(eventData);
        
        // Get phone number from event data or use default
        var phoneNumber = eventData.PhoneNumber ?? GetDefaultPhoneNumber();
        
        if (string.IsNullOrEmpty(phoneNumber))
        {
            Console.WriteLine("❌ SMS.RU: Phone number not specified");
            return;
        }

        try
        {
            var response = SendSms(phoneNumber, message);
            
            if (response.Status == "OK")
            {
                Console.WriteLine($"✅ SMS отправлен через SMS.RU: {eventData.Subject} на {phoneNumber}");
            }
            else
            {
                Console.WriteLine($"❌ SMS.RU API error: {response.StatusText}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка отправки SMS: {ex.Message}");
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

    private string FormatMessage(EventData eventData)
    {
        var sb = new StringBuilder();
        
        if (!string.IsNullOrEmpty(eventData.Subject))
            sb.Append(eventData.Subject);
        
        if (!string.IsNullOrEmpty(eventData.Description))
        {
            if (sb.Length > 0)
                sb.Append(". ");
            sb.Append(eventData.Description);
        }

        return sb.ToString();
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