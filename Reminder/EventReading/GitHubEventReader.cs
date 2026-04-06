using System.Net.Http.Headers;
using System.Text;
using ReminderApp.Common;
using ReminderApp.EventParsing;

namespace ReminderApp.EventReading;

/// <summary>
/// Reads events from a text file stored on GitHub
/// </summary>
public class GitHubEventReader : IEventReader
{
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _filePath;
    private readonly string _branch;
    private readonly Parser _parser = new();

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
    /// Sets the Personal Access Token for authentication
    /// </summary>
    public void SetAuthentication(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<EventData>> ReadEventsAsync()
    {
        try
        {
            var url = $"/repos/{_owner}/{_repo}/contents/{_filePath}?ref={_branch}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ GitHub API error: {response.StatusCode}");
                return new List<EventData>();
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            // Parse the GitHub API response to get file content
            var githubContent = System.Text.Json.JsonSerializer.Deserialize<GitHubFileContent>(jsonContent);
            
            if (githubContent == null || string.IsNullOrEmpty(githubContent.content))
            {
                Console.WriteLine("❌ Could not read file content from GitHub.");
                return new List<EventData>();
            }
            
            // Decode base64 content
            var decodedContent = Encoding.UTF8.GetString(Convert.FromBase64String(githubContent.content));
            
            // Remove newlines from base64 string (GitHub adds them every 64 characters)
            decodedContent = decodedContent.Replace("\n", "").Replace("\r", "");
            
            Console.WriteLine($"📄 Read content from GitHub: {_owner}/{_repo}/{_filePath}");
            
            // Pass content to the existing parser
            return _parser.ParseEvents(decodedContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error fetching from GitHub: {ex.Message}");
            return new List<EventData>();
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