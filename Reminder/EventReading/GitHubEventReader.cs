using System.Net.Http.Headers;
using System.Text;
using ReminderApp.EventParsing;

namespace ReminderApp.EventReading;

/// <summary>
/// Reads events from a text file stored on GitHub
/// </summary>
public class GitHubEventReader : EventReaderBase
{
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _filePath;
    private readonly string _branch;

    public GitHubEventReader(string owner, string repo, string filePath = "events.txt", string branch = "main")
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.github.com");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReminderApp", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        
        _owner = owner;
        _repo = repo;
        _filePath = filePath;
        _branch = branch;
    }

    /// <summary>
    /// Creates GitHubEventReader from a GitHub URL
    /// </summary>
    public static GitHubEventReader FromUrl(string url, string? token = null)
    {
        var (owner, repo, filePath, branch) = ParseGitHubUrl(url);
        
        var reader = new GitHubEventReader(owner, repo, filePath, branch);
        
        if (!string.IsNullOrWhiteSpace(token))
        {
            reader.SetAuthentication(token);
        }
        
        return reader;
    }

    /// <summary>
    /// Sets the Personal Access Token for authentication
    /// </summary>
    public void SetAuthentication(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected override async Task<string?> ReadContentAsync()
    {
        try
        {
            var url = $"/repos/{_owner}/{_repo}/contents/{_filePath}?ref={_branch}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ GitHub API error: {response.StatusCode}");
                return null;
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            // Parse the GitHub API response to get file content
            var githubContent = System.Text.Json.JsonSerializer.Deserialize<GitHubFileContent>(jsonContent);
            
            if (githubContent == null || string.IsNullOrEmpty(githubContent.content))
            {
                Console.WriteLine("❌ Could not read file content from GitHub.");
                return null;
            }
            
            // Decode base64 content
            var decodedContent = Encoding.UTF8.GetString(Convert.FromBase64String(githubContent.content));
            
            // Remove newlines from base64 string (GitHub adds them every 64 characters)
            decodedContent = decodedContent.Replace("\n", "").Replace("\r", "");
            
            Console.WriteLine($"📄 Read content from GitHub: {_owner}/{_repo}/{_filePath}");
            
            return decodedContent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error fetching from GitHub: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses GitHub URL to extract owner, repo, file path, and branch
    /// </summary>
    public static (string owner, string repo, string filePath, string branch) ParseGitHubUrl(string url)
    {
        // Handle formats:
        // https://github.com/owner/repo/blob/main/events.txt
        // https://github.com/owner/repo/blob/master/path/to/file.txt
        // owner/repo (shorthand)
        
        if (string.IsNullOrWhiteSpace(url))
            return (null!, null!, "events.txt", "main");
            
        // Check for shorthand format (owner/repo)
        var parts = url.Split('/');
        if (parts.Length >= 2 && !url.Contains("github.com"))
        {
            var owner = parts[0].Trim();
            var repo = parts[1].Trim();
            var filePath = parts.Length > 2 ? string.Join("/", parts.Skip(2)) : "events.txt";
            return (owner, repo, filePath, "main");
        }
        
        // Parse full GitHub URL
        try
        {
            var uri = new Uri(url);
            if (uri.Host != "github.com" && !uri.Host.EndsWith("github.com"))
                return (null!, null!, "events.txt", "main");
                
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            if (pathParts.Length < 2)
                return (null!, null!, "events.txt", "main");
                
            var owner = pathParts[0];
            var repo = pathParts[1];
            
            // Check for /blob/ branch/filepath
            var filePath = "events.txt";
            var branch = "main";
            
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
            return (null!, null!, "events.txt", "main");
        }
    }
}

/// <summary>
/// Represents GitHub API response for file contents
/// </summary>
public class GitHubFileContent
{
    public string content { get; set; } = string.Empty;
    public string encoding { get; set; } = string.Empty;
}