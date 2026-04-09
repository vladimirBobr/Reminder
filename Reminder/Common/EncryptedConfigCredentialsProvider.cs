using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReminderApp.Common;

/// <summary>
/// Базовый класс провайдера credentials с шифрованием конфига
/// </summary>
public abstract class EncryptedConfigCredentialsProvider<TConfig> where TConfig : class, new()
{
    protected readonly string _configPath;
    protected readonly IDataProtector _protector;

    protected EncryptedConfigCredentialsProvider(string configFileName, string protectorPurpose)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, configFileName);

        var dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(configDir, "keys")));
        _protector = dataProtectionProvider.CreateProtector(protectorPurpose);
    }

    /// <summary>
    /// Загрузить конфиг из файла (токен зашифрован)
    /// </summary>
    protected TConfig? LoadFromFile()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<TConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Расшифровать токен
    /// </summary>
    protected string UnprotectToken(string? encryptedToken)
    {
        if (string.IsNullOrEmpty(encryptedToken))
            return string.Empty;
        
        return _protector.Unprotect(encryptedToken);
    }

    /// <summary>
    /// Зашифровать токен
    /// </summary>
    protected string ProtectToken(string token)
    {
        return _protector.Protect(token);
    }

    /// <summary>
    /// Сохранить конфиг в файл
    /// </summary>
    protected void SaveToFile(TConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}