using OneOf;

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
/// Error fetching file content from GitHub
/// </summary>
public record GitHubFetchError(string Message);

/// <summary>
/// Successful file content fetch
/// </summary>
public record GitHubFetchSuccess(string Content, string Sha);

/// <summary>
/// Error updating file content in GitHub
/// </summary>
public record GitHubUpdateError(string Message);

/// <summary>
/// Successful file update
/// </summary>
public record GitHubUpdateSuccess();

/// <summary>
/// Interface for GitHub API operations
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Get file content from GitHub repository
    /// </summary>
    Task<OneOf<GitHubFetchError, GitHubFetchSuccess>> GetFileContentAsync();

    /// <summary>
    /// Update file content in GitHub repository
    /// </summary>
    /// <param name="content">New content</param>
    /// <param name="sha">File SHA from previous read</param>
    Task<OneOf<GitHubUpdateError, GitHubUpdateSuccess>> UpdateFileContentAsync(string content, string sha);
}