using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReminderApp.Common;

namespace ReminderApp.EventNotification;

public class SmsAeroNotifier : INotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _email;
    private readonly string _apiToken;
    private readonly string _sign;
    private const string BASE_URL = "https://gate.smsaero.ru/v2/sms/send";

    public SmsAeroNotifier(string email, string apiToken, string sign = "SMS Aero")
    {
        _email = email;
        _apiToken = apiToken;
        _sign = sign;

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
            Console.WriteLine("❌ SMSAero: Phone number not specified");
            return;
        }

        try
        {
            var response = SendSms(phoneNumber, message);
            
            if (response.Success)
            {
                Console.WriteLine($"✅ SMS отправлен через SMSAero: {eventData.Subject} на {phoneNumber}");
            }
            else
            {
                Console.WriteLine($"❌ SMSAero API error: {response.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка отправки SMS: {ex.Message}");
        }
    }

    private SmsAeroResponse SendSms(string number, string text)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_email}:{_apiToken}"));
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}?number={Uri.EscapeDataString(number)}&text={Uri.EscapeDataString(text)}&sign={Uri.EscapeDataString(_sign)}");
        request.Headers.Add("Authorization", $"Basic {credentials}");
        request.Headers.Add("Accept", "application/json");

        var response = _httpClient.SendAsync(request).Result;
        
        var content = response.Content.ReadAsStringAsync().Result;
        
        try
        {
            return JsonSerializer.Deserialize<SmsAeroResponse>(content) ?? new SmsAeroResponse { Success = false, ErrorMessage = "Failed to deserialize response" };
        }
        catch
        {
            return new SmsAeroResponse { Success = false, ErrorMessage = content };
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
        return Environment.GetEnvironmentVariable("SMSAERO_DEFAULT_PHONE");
    }
}

public class SmsAeroResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("data")]
    public SmsAeroResponseData? Data { get; set; }
}

public class SmsAeroResponseData
{
    [JsonPropertyName("id")]
    public string? MessageId { get; set; }
    
    [JsonPropertyName("number")]
    public string? Number { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
