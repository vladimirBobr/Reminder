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
        
        if (settings != null)
        {
            // Показываем сохранённые настройки пользователю
            ShowSavedSettings(settings);
            
            Console.Write("Использовать сохранённые настройки? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                DecryptSettings(settings);
                return settings;
            }
        }

        var newSettings = RequestFromConsole();
        SaveToFile(newSettings);
        
        return newSettings;
    }

    /// <summary>
    /// Показать сохранённые настройки пользователю
    /// </summary>
    protected virtual void ShowSavedSettings(TSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("Сохранённые настройки:");
        Console.WriteLine(json);
    }

    /// <summary>
    /// Загрузить настройки из файла
    /// </summary>
    protected TSettings? LoadFromFile()
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
    /// Расшифровать токены в настройках
    /// </summary>
    protected abstract void DecryptSettings(TSettings settings);

    /// <summary>
    /// Зашифровать токены в настройках
    /// </summary>
    protected abstract void EncryptSettings(TSettings settings);

    /// <summary>
    /// Запросить настройки из консоли
    /// </summary>
    protected abstract TSettings RequestFromConsole();

    /// <summary>
    /// Сохранить настройки в файл (с шифрованием)
    /// </summary>
    protected void SaveToFile(TSettings settings)
    {
        EncryptSettings(settings);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
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