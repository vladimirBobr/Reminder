using System.Text.Json;
using System.IO;

namespace ReminderApp.FileStorage;

/// <summary>
/// Stores GitHub configuration (URL and encrypted token) in user's profile directory
/// </summary>
public class GitHubConfigStorage
{
    private readonly string _configPath;
    
    public GitHubConfigStorage()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(userProfile, ".reminder");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "github-config.json");
    }

    public GitHubConfig? Load()
    {
        if (!File.Exists(_configPath))
            return null;
            
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<GitHubConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string githubUrl, string githubToken)
    {
        var encryptedToken = EncryptToken(githubToken);
        
        var config = new GitHubConfig
        {
            GithubUrl = githubUrl,
            EncryptedToken = encryptedToken
        };
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
    
    public string? GetDecryptedToken(string encryptedToken)
    {
        if (string.IsNullOrEmpty(encryptedToken))
            return null;
            
        try
        {
            return DecryptToken(encryptedToken);
        }
        catch
        {
            return null;
        }
    }

    private string EncryptToken(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(tokenBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private string DecryptToken(string encryptedToken)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedToken);
        var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(decryptedBytes);
    }
}

public class GitHubConfig
{
    public string GithubUrl { get; set; } = string.Empty;
    public string EncryptedToken { get; set; } = string.Empty;
}