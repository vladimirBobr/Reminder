using OneOf;
using ReminderApp.GitHubApi;

namespace ReminderApp.EventStorage;

public class NotesService : INotesService
{
    private readonly IGitHubClient _gitHubClient;

    public NotesService(IGitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    public (string Error, string? Message) AddNote(string note, DateOnly? date = null)
    {
        // Step 1: Get file from GitHub
        var fetchResult = _gitHubClient.GetFileContentAsync().Result;
        string? currentContent = null;
        string? sha = null;
        
        fetchResult.Switch(
            error => { },
            success =>
            {
                currentContent = success.Content;
                sha = success.Sha;
            });
        
        if (fetchResult.IsT0)
        {
            return (fetchResult.AsT0.Message, null);
        }

        // Step 2: Modify content
        var (modError, modifiedContent, resultMessage) = NoteModifier.ModifyContent(currentContent!, note, date);
        if (!string.IsNullOrEmpty(modError) || modifiedContent == null)
        {
            return (modError ?? "Failed to modify content", null);
        }

        // Step 3: Update file in GitHub
        var updateResult = _gitHubClient.UpdateFileContentAsync(modifiedContent, sha!).Result;
        
        return updateResult.Match<(string Error, string? Message)>(
            error => (error.Message, null),
            _ => 
            {
                Log.Information("Note added via GitHub API: {Note}, Date: {Date}", note, date?.ToString("dd.MM.yyyy") ?? "none");
                return ("", resultMessage);
            }
        );
    }
}