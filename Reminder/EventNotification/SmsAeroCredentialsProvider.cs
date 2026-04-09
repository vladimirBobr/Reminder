using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReminderApp.EventNotification;

/// <summary>
/// Реализация провайдера credentials для SmsAero
/// Читает из зашифрованного файла, а если файла нет - запрашивает через консоль
/// </summary>
public class SmsAeroCredentialsProvider : ISmsAeroCredentialsProvider
{
    private readonly string _configPath;
    private readonly IDataProtector _protector;

    public SmsAeroCredentialsProvider()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "smsaero-config.json");

        var dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(configDir, "keys")));
        _protector = dataProtectionProvider.CreateProtector("SmsAeroConfig");
    }

    public SmsAeroSettings GetCredentials()
    {
        // Пробуем загрузить из файла
        var config = LoadFromFile();

        if (config != null && !string.IsNullOrEmpty(config.Token))
        {
            Console.WriteLine("✅ Загружены сохраненные SMSAero настройки");

            // Запрашиваем номер телефона если не сохранён
            Console.Write("Введите номер телефона для SMS уведомлений (в формате 79000000000): ");
            var phoneNumber = Console.ReadLine()?.Trim();

            return new SmsAeroSettings(config.Email, config.Token, config.Sign, phoneNumber);
        }

        // Запрашиваем через консоль
        return RequestCredentialsFromConsole();
    }

    private SmsAeroConfig? LoadFromFile()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<SmsAeroConfig>(json);

            if (config != null && !string.IsNullOrEmpty(config.Token))
            {
                // Расшифровываем токен
                config.Token = _protector.Unprotect(config.Token);
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    private void SaveToFile(string email, string apiToken, string sign)
    {
        var encryptedToken = _protector.Protect(apiToken);

        var config = new SmsAeroConfig
        {
            Email = email,
            Token = encryptedToken,
            Sign = sign
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private SmsAeroSettings RequestCredentialsFromConsole()
    {
        Console.WriteLine("Настройка SMSAero:");
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim() ?? "";

        Console.Write("API Token: ");
        var apiToken = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Подпись (по умолчанию SMS Aero): ");
        var sign = Console.ReadLine()?.Trim() ?? "SMS Aero";

        Console.Write("Номер телефона для уведомлений (в формате 79000000000): ");
        var phoneNumber = Console.ReadLine()?.Trim();

        // Сохраняем (кроме номера телефона)
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(apiToken))
        {
            SaveToFile(email, apiToken, sign);
            Console.WriteLine("✅ SMSAero настройки сохранены");
        }

        return new SmsAeroSettings(email, apiToken, sign, phoneNumber);
    }

    private class SmsAeroConfig
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Sign { get; set; } = "SMS Aero";
    }
}