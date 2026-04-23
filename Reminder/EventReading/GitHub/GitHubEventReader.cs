using OneOf;
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
        var result = await _gitHubClient.GetFileContentAsync();
        
        return result.Match<string?>(
            error =>
            {
                Log.Information($"❌ Error reading from GitHub: {error.Message}");
                return null;
            },
            success => success.Content
        );
    }
}