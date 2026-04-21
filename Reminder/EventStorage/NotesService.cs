using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ReminderApp.EventReading.GitHub;

namespace ReminderApp.EventStorage;

public class NotesService : INotesService
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _filePath;
    private readonly string _branch;
    private readonly string _token;

    public NotesService(IGitHubCredentialsProvider credentialsProvider)
    {
        var settings = credentialsProvider.GetCredentials();
        
        _owner = settings.Owner;
        _repo = settings.Repo;
        _filePath = settings.FilePath;
        _branch = settings.Branch;
        _token = settings.Token;
    }

    public string AddNote(string note)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.github.com");
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReminderApp", "1.0"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var url = $"/repos/{_owner}/{_repo}/contents/{_filePath}?ref={_branch}";
        var response = httpClient.GetAsync(url).Result;
        
        if (!response.IsSuccessStatusCode)
        {
            var error = $"Failed to get file from GitHub: {response.StatusCode}";
            Log.Information(error);
            return error;
        }

        var jsonContent = response.Content.ReadAsStringAsync().Result;
        var githubContent = System.Text.Json.JsonSerializer.Deserialize<GitHubFileContent>(jsonContent);
        
        if (githubContent == null || string.IsNullOrEmpty(githubContent.content))
        {
            var error = "Could not read file content from GitHub";
            Log.Information(error);
            return error;
        }

        var currentContent = Encoding.UTF8.GetString(Convert.FromBase64String(githubContent.content));
        var sha = githubContent.sha;

        var lines = currentContent.Split('\n').ToList();
        var insertIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("#notes_section#"))
            {
                insertIndex = i + 1;
                break;
            }
        }

        if (insertIndex == -1)
        {
            var error = "Notes section not found in events file";
            Log.Information(error);
            return error;
        }

        lines.Insert(insertIndex, "");
        lines.Insert(insertIndex + 1, note);
        lines.Insert(insertIndex + 2, "");

        var newContent = string.Join("\n", lines);
        var newContentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(newContent));

        var updateRequest = new
        {
            message = "Add note via admin API",
            content = newContentBase64,
            sha = sha,
            branch = _branch
        };

        var updateResponse = httpClient.PutAsJsonAsync(url, updateRequest).Result;
        
        if (!updateResponse.IsSuccessStatusCode)
        {
            var error = $"Failed to update file on GitHub: {updateResponse.StatusCode}";
            Log.Information(error);
            return error;
        }

        Log.Information("Note added via GitHub API: {Note}", note);
        return "";
    }
}

public class GitHubFileContent
{
    public string content { get; set; } = string.Empty;
    public string sha { get; set; } = string.Empty;
    public string encoding { get; set; } = string.Empty;
}