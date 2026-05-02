using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using OneOf;
using ReminderApp.Common;

namespace ReminderApp.GitHubApi;

/// <summary>
/// GitHub API client implementation
/// </summary>
public class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _filePath;
    private readonly string _branch;

    public GitHubClient(GitHubOptions config)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.github.com");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReminderApp", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        
        // Parse URL to get owner, repo, filePath, branch
        var (owner, repo, filePath, branch) = ParseGitHubUrl(config.Url);
        
        _owner = owner;
        _repo = repo;
        _filePath = filePath;
        _branch = branch;
        
        // Set authentication token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
    }

    /// <inheritdoc />
    public async Task<OneOf<GitHubFetchError, GitHubFetchSuccess>> GetFileContentAsync()
    {
        try
        {
            var url = $"/repos/{_owner}/{_repo}/contents/{_filePath}?ref={_branch}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                Log.Information($"❌ GitHub API error: {response.StatusCode}");
                return new GitHubFetchError($"GitHub API error: {response.StatusCode}");
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            var githubContent = System.Text.Json.JsonSerializer.Deserialize<GitHubFileContent>(jsonContent);
            
            if (githubContent == null || string.IsNullOrEmpty(githubContent.content))
            {
                Log.Information("❌ Could not read file content from GitHub.");
                return new GitHubFetchError("Could not read file content from GitHub");
            }
            
            var content = Encoding.UTF8.GetString(Convert.FromBase64String(githubContent.content));

            Log.Information($"📄 Read content from GitHub: {_owner}/{_repo}/{_filePath}");
            
            return new GitHubFetchSuccess(content, githubContent.sha);
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Error fetching from GitHub: {ex.Message}");
            return new GitHubFetchError($"Error fetching from GitHub: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<OneOf<GitHubUpdateError, GitHubUpdateSuccess>> UpdateFileContentAsync(string content, string sha)
    {
        try
        {
            var url = $"/repos/{_owner}/{_repo}/contents/{_filePath}?ref={_branch}";
            var newContentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

            var updateRequest = new
            {
                message = "Update via ReminderApp",
                content = newContentBase64,
                sha = sha,
                branch = _branch
            };

            var response = await _httpClient.PutAsJsonAsync(url, updateRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = $"Failed to update file on GitHub: {response.StatusCode}";
                Log.Information(error);
                return new GitHubUpdateError(error);
            }

            Log.Information($"📝 Updated content on GitHub: {_owner}/{_repo}/{_filePath}");
            return new GitHubUpdateSuccess();
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Error updating GitHub: {ex.Message}");
            return new GitHubUpdateError($"Error updating GitHub: {ex.Message}");
        }
    }

    private static (string owner, string repo, string filePath, string branch) ParseGitHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (string.Empty, string.Empty, string.Empty, string.Empty);

        try
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            
            if (pathParts.Length < 2)
                return (string.Empty, string.Empty, string.Empty, string.Empty);

            var owner = pathParts[0];
            var repo = pathParts[1];
            var filePath = string.Empty;
            var branch = string.Empty;

            var blobIndex = Array.IndexOf(pathParts, "blob");
            if (blobIndex >= 0 && blobIndex + 2 < pathParts.Length)
            {
                branch = pathParts[blobIndex + 1];
                filePath = string.Join("/", pathParts.Skip(blobIndex + 2));
            }

            return (owner, repo, filePath, branch);
        }
        catch
        {
            return (string.Empty, string.Empty, string.Empty, string.Empty);
        }
    }
}
