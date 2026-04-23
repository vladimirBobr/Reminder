namespace ReminderApp.GitHubApi;

/// <summary>
/// Represents GitHub API response for file contents
/// </summary>
public class GitHubFileContent
{
    public string content { get; set; } = string.Empty;
    public string sha { get; set; } = string.Empty;
    public string encoding { get; set; } = string.Empty;
}

/// <summary>
/// Interface for GitHub API operations
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Get file content from GitHub repository
    /// </summary>
    /// <returns>Tuple of (Error, Content, Sha)</returns>
    Task<(string? Error, string? Content, string? Sha)> GetFileContentAsync();

    /// <summary>
    /// Update file content in GitHub repository
    /// </summary>
    /// <param name="content">New content</param>
    /// <param name="sha">File SHA from previous read</param>
    /// <returns>Error message or null on success</returns>
    Task<string?> UpdateFileContentAsync(string content, string sha);
}