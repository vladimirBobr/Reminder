using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ReminderApp.FileStorage;

/// <summary>
/// Stores SMSAero configuration (email, encrypted token, sign) in user's profile directory
/// </summary>
public class SmsAeroConfigStorage
{
    private readonly string _configPath;
    
    public SmsAeroConfigStorage()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "smsaero-config.json");
    }

    public SmsAeroConfig? Load()
    {
        if (!File.Exists(_configPath))
            return null;
            
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<SmsAeroConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string email, string apiToken, string sign)
    {
        var encryptedToken = EncryptToken(apiToken);
        
        var config = new SmsAeroConfig
        {
            Email = email,
            EncryptedToken = encryptedToken,
            Sign = sign
        };
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
    
    public string? GetDecryptedToken(string encryptedToken)
    {
        if (string.IsNullOrEmpty(encryptedToken))
            return null;
            
        try
        {
            return DecryptToken(encryptedToken);
        }
        catch
        {
            return null;
        }
    }

    private string EncryptToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = ProtectedData.Protect(tokenBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private string DecryptToken(string encryptedToken)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedToken);
        var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}

public class SmsAeroConfig
{
    public string Email { get; set; } = string.Empty;
    public string EncryptedToken { get; set; } = string.Empty;
    public string Sign { get; set; } = "SMS Aero";
}