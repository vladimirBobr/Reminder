using ReminderApp.FileStorage;

namespace ReminderApp.EventReading;

/// <summary>
/// DTO for parsed GitHub URL components
/// </summary>
public class GitHubUrlParts
{
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

/// <summary>
/// Factory for creating GitHubEventReader instances with proper configuration
/// </summary>
public class GitHubEventReaderFactory
{
    private readonly GitHubConfigStorage _configStorage;

    public GitHubEventReaderFactory(GitHubConfigStorage configStorage)
    {
        _configStorage = configStorage ?? throw new ArgumentNullException(nameof(configStorage));
    }

    /// <summary>
    /// Creates a GitHubEventReader, prompting for credentials if needed
    /// Returns null on error (errors are printed to console)
    /// </summary>
    public GitHubEventReader? Create()
    {
        try
        {
            var (url, token) = GetCredentials();
            return Create(url, token);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a GitHubEventReader from URL and token
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when URL or token is invalid</exception>
    public GitHubEventReader Create(string url, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        var parsed = ParseGitHubUrl(url);

        var reader = new GitHubEventReader(
            owner: parsed.Owner,
            repo: parsed.Repo,
            filePath: parsed.FilePath,
            branch: parsed.Branch);

        reader.SetAuthentication(token);

        return reader;
    }

    /// <summary>
    /// Parses GitHub URL to extract owner, repo, file path, and branch
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    public GitHubUrlParts ParseGitHubUrl(string url)
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

                filePath = parts.Length > 2 ? string.Join("/", parts.Skip(2)) : "";
                branch = "";

                return new GitHubUrlParts { Owner = owner, Repo = repo, FilePath = filePath, Branch = branch };
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
        filePath = "";
        branch = "";

        var blobIndex = Array.IndexOf(pathParts, "blob");
        if (blobIndex >= 0 && blobIndex + 2 < pathParts.Length)
        {
            branch = pathParts[blobIndex + 1];
            filePath = string.Join("/", pathParts.Skip(blobIndex + 2));
        }

        return new GitHubUrlParts { Owner = owner, Repo = repo, FilePath = filePath, Branch = branch };
    }

    private (string url, string token) GetCredentials()
    {
        var config = _configStorage.Load();

        if (config != null)
        {
            return GetCredentialsFromSavedConfig(config);
        }

        return GetCredentialsFromInput(savePrompt: true);
    }

    private (string url, string token) GetCredentialsFromSavedConfig(GitHubConfig config)
    {
        Console.WriteLine($"Saved URL found: {config.GithubUrl}");
        Console.Write("Use saved credentials? (y/n): ");
        var useSaved = Console.ReadLine()?.ToLower() == "y";

        if (useSaved)
        {
            var token = _configStorage.GetDecryptedToken(config.EncryptedToken) ?? "";

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Failed to decrypt saved token. Please enter new credentials.");
                return GetCredentialsFromInput(savePrompt: false);
            }

            return (config.GithubUrl, token);
        }

        return GetCredentialsFromInput(savePrompt: true);
    }

    private (string url, string token) GetCredentialsFromInput(bool savePrompt)
    {
        Console.Write("Enter GitHub file URL (e.g., https://github.com/owner/repo/blob/main/events.txt): ");
        var url = Console.ReadLine() ?? "";
        Console.Write("Enter GitHub Personal Access Token: ");
        var token = Console.ReadLine() ?? "";

        if (savePrompt)
        {
            Console.Write("Save credentials? (y/n): ");
            var save = Console.ReadLine()?.ToLower() == "y";
            if (save)
            {
                _configStorage.Save(url, token);
                Console.WriteLine("✅ Credentials saved.");
            }
        }

        return (url, token);
    }
}