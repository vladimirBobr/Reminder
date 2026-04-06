using System.Net.Http.Headers;
using System.Text;

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
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    public static GitHubEventReader FromUrl(string url, string? token = null)
    {
        var parsed = ParseGitHubUrl(url);
        
        var reader = new GitHubEventReader(owner: parsed.Item1, repo: parsed.Item2, filePath: parsed.Item3, branch: parsed.Item4);
        
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
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    public static (string, string, string, string) ParseGitHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));
        
        string owner;
        string repo;
        string filePath;
        string branch;
        
        // Check for shorthand format (owner/repo)
        if (!url.Contains("github.com"))
        {
            var parts = url.Split('/');
            if (parts.Length >= 2)
            {
                owner = parts[0].Trim();
                repo = parts[1].Trim();
                
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                    throw new ArgumentException($"Invalid URL format: {url}", nameof(url));
                
                filePath = parts.Length > 2 ? string.Join("/", parts.Skip(2)) : "events.txt";
                branch = "main";
                
                return (owner, repo, filePath, branch);
            }
        }
        
        // Parse full GitHub URL
        var uri = new Uri(url);
        if (uri.Host != "github.com" && !uri.Host.EndsWith("github.com"))
            throw new ArgumentException($"Invalid GitHub host: {uri.Host}", nameof(url));
            
        var pathParts = uri.AbsolutePath.Trim('/').Split('/');
        if (pathParts.Length < 2)
            throw new ArgumentException($"Invalid GitHub URL path: {uri.AbsolutePath}", nameof(url));
            
        owner = pathParts[0];
        repo = pathParts[1];
        
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            throw new ArgumentException($"Invalid repository: {url}", nameof(url));
        
        // Check for /blob/ branch/filepath
        filePath = "events.txt";
        branch = "main";
        
        var blobIndex = Array.IndexOf(pathParts, "blob");
        if (blobIndex >= 0 && blobIndex + 2 < pathParts.Length)
        {
            branch = pathParts[blobIndex + 1];
            filePath = string.Join("/", pathParts.Skip(blobIndex + 2));
        }
        
        return (owner, repo, filePath, branch);
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