using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;

namespace ReminderApp.FileStorage;

/// <summary>
/// Stores GitHub configuration (URL and encrypted token) in user's profile directory
/// </summary>
public class GitHubConfigStorage
{
    private readonly string _configPath;
    private readonly IDataProtector _protector;

    public GitHubConfigStorage()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "github-config.json");

        var dataProtectionProvider = DataProtectionProvider.Create(
            new DirectoryInfo(Path.Combine(configDir, "keys")));
        _protector = dataProtectionProvider.CreateProtector("GitHubConfig");
    }

    public GitHubConfig? Load()
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

    public void Save(string githubUrl, string token)
    {
        var encryptedToken = _protector.Protect(token);

        var config = new GitHubConfig
        {
            GithubUrl = githubUrl,
            Token = encryptedToken
        };
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}

public class GitHubConfig
{
    public string GithubUrl { get; set; } = string.Empty;
    public string EncryptedToken { get; set; } = string.Empty;
    [JsonIgnore]
    public string Token { get; set; } = string.Empty;
}
