using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReminderApp.EventNotification.SmsAero;

public class SmsAeroNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _email;
    private readonly string _apiToken;
    private readonly string _sign;
    private const string BASE_URL = "https://gate.smsaero.ru/v2/sms/send";
    private const string SIGN = "SMS Aero";

    public SmsAeroNotifier(ISmsAeroCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _email = settings.Email;
        _apiToken = settings.ApiToken;
        _sign = settings.Sign;

        // Сохраняем номер телефона для использования в уведомлениях
        if (!string.IsNullOrEmpty(settings.PhoneNumber))
        {
            Environment.SetEnvironmentVariable("SMSAERO_DEFAULT_PHONE", settings.PhoneNumber);
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

    public async Task NotifyAsync(string message)
    {
        var phoneNumber = GetDefaultPhoneNumber();
        
        if (string.IsNullOrEmpty(phoneNumber))
        {
            Log.Information("❌ SMSAero: Phone number not specified");
            return;
        }

        try
        {
            var error = await SendSmsAsync(phoneNumber, message);
            
            if (string.IsNullOrEmpty(error))
            {
                Log.Information($"✅ SMS отправлен через SMSAero на {phoneNumber}");
            }
            else
            {
                Log.Information($"❌ SMSAero API error: {error}");
            }
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Ошибка отправки SMS: {ex.Message}");
        }
    }

    private async Task<string?> SendSmsAsync(string number, string text)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_email}:{_apiToken}"));

        var url = $"{BASE_URL}?text={Uri.EscapeDataString(text)}&sign={Uri.EscapeDataString(SIGN)}&number={Uri.EscapeDataString(number)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("Authorization", $"Basic {credentials}");
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request);
        
        var content = await response.Content.ReadAsStringAsync();
        
        try
        {
            var responseJson = JsonSerializer.Deserialize<SmsAeroResponse>(content);
            return responseJson?.Success == true
                ? null
                : content;
        }
        catch(Exception ex)
        {
            return ex.ToString();
        }
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
}    