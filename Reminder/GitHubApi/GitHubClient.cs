using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using OneOf;

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

    public GitHubClient(IGitHubCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.github.com");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReminderApp", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        
        _owner = settings.Owner;
        _repo = settings.Repo;
        _filePath = settings.FilePath;
        _branch = settings.Branch;
        
        // Set authentication token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Token);
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
}