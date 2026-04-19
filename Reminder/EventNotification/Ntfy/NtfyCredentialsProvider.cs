namespace ReminderApp.EventNotification.Ntfy;

/// <summary>
/// Implementation of credentials provider for Ntfy
/// </summary>
public class NtfyCredentialsProvider : INtfyCredentialsProvider
{
    public NtfySettings GetCredentials()
    {
        // 1. Пытаемся получить из переменных окружения
        var serverUrl = Environment.GetEnvironmentVariable("NTFY_SERVER_URL");
        var topic = Environment.GetEnvironmentVariable("NTFY_TOPIC");
        var username = Environment.GetEnvironmentVariable("NTFY_USERNAME");
        var password = Environment.GetEnvironmentVariable("NTFY_PASSWORD");

        // Если все переменные есть — возвращаем
        if (!string.IsNullOrEmpty(serverUrl) && !string.IsNullOrEmpty(topic))
        {
            Log.Information("✅ Ntfy настройки загружены из переменных окружения. {ServerUrl}, {Topic}", serverUrl, topic);
            return new NtfySettings
            {
                ServerUrl = serverUrl,
                Topic = topic,
                Username = username,
                Password = password
            };
        }

        // 2. Только в DEBUG режиме — запрашиваем из консоли
        if (DebugHelper.IsDebug)
        {
            Log.Information("Ntfy настройки не найдены. Введите их:");
            var settings = RequestFromConsole();

            // Сохраняем в переменные окружения для текущей сессии
            Environment.SetEnvironmentVariable("NTFY_SERVER_URL", settings.ServerUrl, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("NTFY_TOPIC", settings.Topic, EnvironmentVariableTarget.User);

            if (!string.IsNullOrEmpty(settings.Username))
                Environment.SetEnvironmentVariable("NTFY_USERNAME", settings.Username, EnvironmentVariableTarget.User);

            if (!string.IsNullOrEmpty(settings.Password))
                Environment.SetEnvironmentVariable("NTFY_PASSWORD", settings.Password, EnvironmentVariableTarget.User);

            return settings;
        }

        // 3. В RELEASE режиме — ошибка, если переменных нет
        throw new InvalidOperationException(
            "Ntfy credentials not found. Set NTFY_SERVER_URL and NTFY_TOPIC environment variables."
        );
    }

    private NtfySettings RequestFromConsole()
    {
        Log.Information("Ntfy configuration:");
        Console.Write("Server URL: ");
        var serverUrl = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Topic: ");
        var topic = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Username (optional, press Enter to skip): ");
        var username = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Password (optional, press Enter to skip): ");
        var password = Console.ReadLine()?.Trim() ?? "";

        Log.Information("✅ Ntfy settings saved");

        return new NtfySettings 
        { 
            ServerUrl = serverUrl, 
            Topic = topic,
            Username = string.IsNullOrEmpty(username) ? null : username,
            Password = string.IsNullOrEmpty(password) ? null : password
        };
    }
}
