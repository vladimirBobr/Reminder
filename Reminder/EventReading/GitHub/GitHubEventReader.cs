using ReminderApp.EventReading.LocalFile;
using ReminderApp.GitHubApi;

namespace ReminderApp.EventReading.GitHub;

/// <summary>
/// Reads events from a text file stored on GitHub
/// </summary>
public class GitHubEventReader : EventReaderBase
{
    private readonly IGitHubClient _gitHubClient;

    public GitHubEventReader(IGitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    protected override async Task<string?> ReadContentAsync()
    {
        var (error, content, _) = await _gitHubClient.GetFileContentAsync();
        
        if (!string.IsNullOrEmpty(error))
        {
            Log.Information($"❌ Error reading from GitHub: {error}");
            return null;
        }
        
        return content;
    }
}