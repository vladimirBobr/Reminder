using ReminderApp.Common;

namespace ReminderApp.EventReading;

/// <summary>
/// Реализация провайдера credentials для GitHub
/// </summary>
public class GitHubCredentialsProvider : 
    EncryptedConfigCredentialsProvider<GitHubSettings>, 
    IGitHubCredentialsProvider
{
    // DTO для хранения в файле (с зашифрованным токеном)
    private class GitHubStoredSettings
    {
        public string GithubUrl { get; set; } = string.Empty;
        public string EncryptedToken { get; set; } = string.Empty;
    }

    public GitHubCredentialsProvider() 
        : base("github-config.json", "GitHubConfig")
    {
    }

    protected override bool HasValidSettings(GitHubSettings settings)
    {
        return !string.IsNullOrEmpty(settings.Url);
    }

    protected override void DecryptSettings(GitHubSettings settings)
    {
        settings.Token = UnprotectToken(settings.Token);
    }

    protected override void EncryptSettings(GitHubSettings settings)
    {
        settings.Token = ProtectToken(settings.Token);
    }

    protected override GitHubSettings RequestFromConsole()
    {
        Console.Write("Enter GitHub file URL (e.g., https://github.com/owner/repo/blob/main/events.txt): ");
        var url = Console.ReadLine() ?? "";
        Console.Write("Enter GitHub Personal Access Token: ");
        var token = Console.ReadLine() ?? "";

        Console.Write("Save credentials? (y/n): ");
        var save = Console.ReadLine()?.ToLower() == "y";
        
        if (save)
        {
            Console.WriteLine("✅ Credentials saved.");
        }

        var parsed = ParseGitHubUrl(url);
        return new GitHubSettings
        {
            Url = url,
            Token = token,
            Owner = parsed.Owner,
            Repo = parsed.Repo,
            FilePath = parsed.FilePath,
            Branch = parsed.Branch
        };
    }

    protected override sealed GitHubSettings? LoadFromFile()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            var stored = System.Text.Json.JsonSerializer.Deserialize<GitHubStoredSettings>(json);
            
            if (stored == null)
                return null;

            Console.WriteLine($"Saved URL found: {stored.GithubUrl}");
            Console.Write("Use saved credentials? (y/n): ");
            var useSaved = Console.ReadLine()?.ToLower() == "y";

            if (!useSaved)
                return null;

            var decryptedToken = UnprotectToken(stored.EncryptedToken);
            if (string.IsNullOrEmpty(decryptedToken))
            {
                Console.WriteLine("❌ Failed to decrypt saved token. Please enter new credentials.");
                return null;
            }

            var parsed = ParseGitHubUrl(stored.GithubUrl);
            return new GitHubSettings
            {
                Url = stored.GithubUrl,
                Token = decryptedToken,
                Owner = parsed.Owner,
                Repo = parsed.Repo,
                FilePath = parsed.FilePath,
                Branch = parsed.Branch
            };
        }
        catch
        {
            return null;
        }
    }

    private GitHubUrlParts ParseGitHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        string owner;
        string repo;
        string filePath;
        string branch;

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
}

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