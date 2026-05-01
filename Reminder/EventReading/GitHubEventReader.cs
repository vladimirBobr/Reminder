using ReminderApp.GitHubApi;
using ReminderApp.EventReading.Parsers;

namespace ReminderApp.EventReading;

/// <summary>
/// IEventReader implementation that fetches and parses events from GitHub
/// </summary>
public class GitHubEventReader : IEventReader
{
    private static readonly ILogger _log = Log.ForContext<GitHubEventReader>();
    private readonly IGitHubClient _gitHubClient;
    private readonly IYamlParser _yamlParser;

    public GitHubEventReader(IGitHubClient gitHubClient, IYamlParser yamlParser)
    {
        _gitHubClient = gitHubClient;
        _yamlParser = yamlParser;
    }

    public async Task<ParsedFileData> ReadEventsAsync()
    {
        var result = await _gitHubClient.GetFileContentAsync();

        return result.Match(
            error =>
            {
                _log.Error("❌ Failed to fetch events from GitHub: {Error}", error.Message);
                throw new InvalidOperationException($"Failed to fetch events from GitHub: {error.Message}");
            },
            success =>
            {
                _log.Information("📄 Parsing YAML content from GitHub ({Length} chars)", success.Content.Length);
                return _yamlParser.Parse(success.Content);
            }
        );
    }
}