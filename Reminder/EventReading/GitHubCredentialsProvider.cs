using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReminderApp.EventReading;

/// <summary>
/// Реализация провайдера credentials для GitHub
/// Читает из зашифрованного файла, а если файла нет - запрашивает через консоль
/// </summary>
public class GitHubCredentialsProvider : IGitHubCredentialsProvider
{
    private readonly string _configPath;
    private readonly IDataProtector _protector;

    public GitHubCredentialsProvider()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "github-config.json");

        var dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(configDir, "keys")));
        _protector = dataProtectionProvider.CreateProtector("GitHubConfig");
    }

    public GitHubSettings GetCredentials()
    {
        var config = LoadFromFile();

        if (config != null)
        {
            return GetCredentialsFromSavedConfig(config);
        }

        return GetCredentialsFromInput(savePrompt: true);
    }

    private GitHubConfig? LoadFromFile()
    {
        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<GitHubConfig>(json);

            if (config != null && !string.IsNullOrEmpty(config.EncryptedToken))
            {
                config.Token = _protector.Unprotect(config.EncryptedToken);
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    private void SaveToFile(string githubUrl, string token)
    {
        var encryptedToken = _protector.Protect(token);

        var config = new GitHubConfig
        {
            GithubUrl = githubUrl,
            EncryptedToken = encryptedToken
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private GitHubSettings GetCredentialsFromSavedConfig(GitHubConfig config)
    {
        Console.WriteLine($"Saved URL found: {config.GithubUrl}");
        Console.Write("Use saved credentials? (y/n): ");
        var useSaved = Console.ReadLine()?.ToLower() == "y";

        if (useSaved)
        {
            if (string.IsNullOrEmpty(config.Token))
            {
                Console.WriteLine("❌ Failed to decrypt saved token. Please enter new credentials.");
                return GetCredentialsFromInput(savePrompt: false);
            }

            var parsed = ParseGitHubUrl(config.GithubUrl);
            return new GitHubSettings(
                config.GithubUrl,
                config.Token,
                parsed.Owner,
                parsed.Repo,
                parsed.FilePath,
                parsed.Branch
            );
        }

        return GetCredentialsFromInput(savePrompt: true);
    }

    private GitHubSettings GetCredentialsFromInput(bool savePrompt)
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
                SaveToFile(url, token);
                Console.WriteLine("✅ Credentials saved.");
            }
        }

        var parsed = ParseGitHubUrl(url);
        return new GitHubSettings(
            url,
            token,
            parsed.Owner,
            parsed.Repo,
            parsed.FilePath,
            parsed.Branch
        );
    }

    private GitHubUrlParts ParseGitHubUrl(string url)
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

/// <summary>
/// Internal config class for file storage
/// </summary>
internal class GitHubConfig
{
    public string GithubUrl { get; set; } = string.Empty;
    public string EncryptedToken { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}