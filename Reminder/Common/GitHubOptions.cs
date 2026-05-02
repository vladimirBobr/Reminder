namespace ReminderApp.Common;

/// <summary>
/// GitHub configuration section in appconfig.json
/// </summary>
public class GitHubOptions
{
    public string Url { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Validate that all required fields are set
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            throw new InvalidOperationException("GitHub URL is not configured. Set GITHUB_URL environment variable.");
        }
        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new InvalidOperationException("GitHub Token is not configured. Set GITHUB_TOKEN environment variable.");
        }
    }
}