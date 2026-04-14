using ReminderApp.Common;

namespace ReminderApp.EventNotification.Ntfy;

/// <summary>
/// Implementation of credentials provider for Ntfy
/// </summary>
public class NtfyCredentialsProvider : 
    EncryptedConfigCredentialsProvider<NtfySettings>, 
    INtfyCredentialsProvider
{
    public NtfyCredentialsProvider() 
        : base("ntfy-config.json", "NtfyConfig")
    {
    }

    protected override void DecryptSettings(NtfySettings settings)
    {
        if (!string.IsNullOrEmpty(settings.Username))
            settings.Username = UnprotectToken(settings.Username);
        
        if (!string.IsNullOrEmpty(settings.Password))
            settings.Password = UnprotectToken(settings.Password);
    }

    protected override void EncryptSettings(NtfySettings settings)
    {
        if (!string.IsNullOrEmpty(settings.Username))
            settings.Username = ProtectToken(settings.Username);
        
        if (!string.IsNullOrEmpty(settings.Password))
            settings.Password = ProtectToken(settings.Password);
    }

    protected override NtfySettings RequestFromConsole()
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
