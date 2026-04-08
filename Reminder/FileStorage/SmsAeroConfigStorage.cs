using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReminderApp.FileStorage;

/// <summary>
/// Stores SMSAero configuration (email, encrypted token, sign) in user's profile directory
/// </summary>
public class SmsAeroConfigStorage
{
    private readonly string _configPath;
    private readonly IDataProtector _protector;

    public SmsAeroConfigStorage()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "smsaero-config.json");

        var dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(configDir, "keys")));
        _protector = dataProtectionProvider.CreateProtector("SmsAeroConfig");
    }

    public SmsAeroConfig? Load()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<SmsAeroConfig>(json);

            if (config != null && !string.IsNullOrEmpty(config.Token))
            {
                // Расшифровываем токен сразу при загрузке
                config.Token = _protector.Unprotect(config.Token);
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    public void Save(string email, string apiToken, string sign)
    {
        var encryptedToken = _protector.Protect(apiToken);

        var config = new
        {
            Email = email,
            Token = encryptedToken,
            Sign = sign
        };
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}

public class SmsAeroConfig
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Sign { get; set; } = "SMS Aero";
}
