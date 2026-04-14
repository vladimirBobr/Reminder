namespace ReminderApp.EventNotification.Ntfy;

/// <summary>
/// DTO for Ntfy settings
/// </summary>
public record NtfySettings
{
    public string ServerUrl { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Interface for getting Ntfy settings
/// </summary>
public interface INtfyCredentialsProvider
{
    /// <summary>
    /// Get Ntfy settings (load from file or request via console)
    /// </summary>
    NtfySettings GetCredentials();
}