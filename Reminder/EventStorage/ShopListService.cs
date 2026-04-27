using OneOf;
using ReminderApp.GitHubApi;

namespace ReminderApp.EventStorage;

public class ShopListService : IShopListService
{
    private readonly IGitHubClient _gitHubClient;

    public ShopListService(IGitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    public (string Error, string? Message) AddItem(string item)
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
        var modResult = ShopListModifier.ModifyContent(currentContent!, item);
        
        return modResult.Match<(string Error, string? Message)>(
            error => (error.Message, null),
            success =>
            {
                // Step 3: Update file in GitHub
                var updateResult = _gitHubClient.UpdateFileContentAsync(success.ModifiedContent, sha!).Result;
                
                return updateResult.Match<(string Error, string? Message)>(
                    updateError => (updateError.Message, null),
                    _ => 
                    {
                        Log.Information("Shopping item added via GitHub API: {Item}", item);
                        return ("", success.ResultMessage);
                    }
                );
            }
        );
    }
}