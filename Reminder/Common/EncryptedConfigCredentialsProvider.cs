using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReminderApp.Common;

/// <summary>
/// Базовый класс провайдера credentials с шифрованием конфига
/// </summary>
public abstract class EncryptedConfigCredentialsProvider<TSettings> where TSettings : class
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
    /// Основной метод получения credentials
    /// </summary>
    public TSettings GetCredentials()
    {
        var settings = LoadFromFile();

        if (settings != null && HasValidSettings(settings))
        {
            DecryptSettings(settings);
            return settings;
        }

        settings = RequestFromConsole();
        SaveToFile(settings);
        
        return settings;
    }

    /// <summary>
    /// Проверить, есть ли валидные настройки
    /// </summary>
    protected abstract bool HasValidSettings(TSettings settings);

    /// <summary>
    /// Расшифровать токены в настройках
    /// </summary>
    protected abstract void DecryptSettings(TSettings settings);

    /// <summary>
    /// Зашифровать токены в настройках перед сохранением
    /// </summary>
    protected abstract void EncryptSettings(TSettings settings);

    /// <summary>
    /// Запросить настройки из консоли
    /// </summary>
    protected abstract TSettings RequestFromConsole();

    /// <summary>
    /// Загрузить настройки из файла (переопределяется для кастомной десериализации)
    /// </summary>
    protected virtual TSettings? LoadFromFile()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<TSettings>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Сохранить настройки в файл
    /// </summary>
    protected void SaveToFile(TSettings settings)
    {
        EncryptSettings(settings);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
        // После сохранения расшифровываем обратно для использования
        DecryptSettings(settings);
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
}