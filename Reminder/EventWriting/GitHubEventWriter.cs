using ReminderApp.Common;
using ReminderApp.EventReading.Parsers;
using ReminderApp.GitHubApi;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReminderApp.EventWriting;

public class GitHubEventWriter : IEventWriter
{
    private static readonly ILogger _log = Log.ForContext<GitHubEventWriter>();
    private readonly IGitHubClient _gitHubClient;
    private readonly IYamlParser _yamlParser;

    public GitHubEventWriter(IGitHubClient gitHubClient, IYamlParser yamlParser)
    {
        _gitHubClient = gitHubClient;
        _yamlParser = yamlParser;
    }

    public async Task<EventWriteResult> UpdateEventDateAsync(string key, DateOnly newDate)
    {
        try
        {
            // 1. Get current file content and SHA
            var result = await _gitHubClient.GetFileContentAsync();
            
            string? content = null;
            string? sha = null;
            
            if (result.IsT0)
            {
                _log.Error("❌ Failed to fetch events: {Error}", result.AsT0.Message);
            }
            else
            {
                content = result.AsT1.Content;
                sha = result.AsT1.Sha;
            }
            
            if (content == null)
                return new EventWriteResult(false, "Failed to fetch events from GitHub");
            
            if (sha == null)
                return new EventWriteResult(false, "Could not get file SHA from GitHub");

            // 2. Parse YAML
            var parsedData = _yamlParser.Parse(content);

            // 3. Find event by key and update date
            var eventFound = false;
            string? newKey = null;
            foreach (var evt in parsedData.Events)
            {
                if (evt.GetKey() == key)
                {
                    evt.Date = newDate;
                    eventFound = true;
                    newKey = evt.GetKey();
                    _log.Information("📅 Updated event {Key} to date {Date}, new key: {NewKey}", key, newDate, newKey);
                    break;
                }
            }

            if (!eventFound)
            {
                _log.Warning("❌ Event with key {Key} not found", key);
                return new EventWriteResult(false, $"Event with key '{key}' not found");
            }

            // 4. Serialize back to YAML
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var yamlOutput = serializer.Serialize(new
            {
                events = parsedData.Events.Select(e => new
                {
                    date = e.Date.ToString("yyyy-MM-dd"),
                    time = e.Time?.ToString("HH:mm"),
                    subject = e.Subject,
                    description = e.Description
                }),
                shopping = parsedData.ShoppingItems.Select(s => s.Subject)
            });

            // 5. Update file on GitHub
            var updateResult = await _gitHubClient.UpdateFileContentAsync(yamlOutput, sha);
            
            return updateResult.Match(
                error =>
                {
                    _log.Error("❌ Failed to update GitHub: {Error}", error.Message);
                    return new EventWriteResult(false, error.Message);
                },
                _ =>
                {
                    _log.Information("✅ Successfully updated file on GitHub");
                    return new EventWriteResult(true, null, newKey);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error updating event date");
            return new EventWriteResult(false, ex.Message);
        }
    }

    public async Task<EventWriteResult> UpdateEventAsync(string key, string? subject, string? description)
    {
        try
        {
            // 1. Get current file content and SHA
            var result = await _gitHubClient.GetFileContentAsync();
            
            string? content = null;
            string? sha = null;
            
            if (result.IsT0)
            {
                _log.Error("❌ Failed to fetch events: {Error}", result.AsT0.Message);
                return new EventWriteResult(false, "Failed to fetch events from GitHub");
            }
            else
            {
                content = result.AsT1.Content;
                sha = result.AsT1.Sha;
            }
            
            if (content == null)
                return new EventWriteResult(false, "Failed to fetch events from GitHub");
            
            if (sha == null)
                return new EventWriteResult(false, "Could not get file SHA from GitHub");

            // 2. Parse YAML
            var parsedData = _yamlParser.Parse(content);

            // 3. Find event by key and update
            var eventFound = false;
            string? newKey = null;
            foreach (var evt in parsedData.Events)
            {
                if (evt.GetKey() == key)
                {
                    evt.Subject = subject;
                    evt.Description = description;
                    eventFound = true;
                    newKey = evt.GetKey();
                    _log.Information("✏️ Updated event {Key}: subject={Subject}, desc={Desc}, new key: {NewKey}", key, subject, description, newKey);
                    break;
                }
            }

            if (!eventFound)
            {
                _log.Warning("❌ Event with key {Key} not found", key);
                return new EventWriteResult(false, $"Event with key '{key}' not found");
            }

            // 4. Serialize back to YAML
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var yamlOutput = serializer.Serialize(new
            {
                events = parsedData.Events.Select(e => new
                {
                    date = e.Date.ToString("yyyy-MM-dd"),
                    time = e.Time?.ToString("HH:mm"),
                    subject = e.Subject,
                    description = e.Description
                }),
                shopping = parsedData.ShoppingItems.Select(s => s.Subject)
            });

            // 5. Update file on GitHub
            var updateResult = await _gitHubClient.UpdateFileContentAsync(yamlOutput, sha);
            
            return updateResult.Match(
                error =>
                {
                    _log.Error("❌ Failed to update GitHub: {Error}", error.Message);
                    return new EventWriteResult(false, error.Message);
                },
                _ =>
                {
                    _log.Information("✅ Successfully updated event on GitHub");
                    return new EventWriteResult(true, null, newKey);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error updating event");
            return new EventWriteResult(false, ex.Message);
        }
    }

    public async Task<EventWriteResult> DeleteEventAsync(string key)
    {
        try
        {
            // 1. Get current file content and SHA
            var result = await _gitHubClient.GetFileContentAsync();
            
            string? content = null;
            string? sha = null;
            
            if (result.IsT0)
            {
                _log.Error("❌ Failed to fetch events: {Error}", result.AsT0.Message);
                return new EventWriteResult(false, "Failed to fetch events from GitHub");
            }
            else
            {
                content = result.AsT1.Content;
                sha = result.AsT1.Sha;
            }
            
            if (content == null)
                return new EventWriteResult(false, "Failed to fetch events from GitHub");
            
            if (sha == null)
                return new EventWriteResult(false, "Could not get file SHA from GitHub");

            // 2. Parse YAML
            var parsedData = _yamlParser.Parse(content);

            // 3. Find event by key and remove it
            var eventFound = false;
            var eventsToRemove = parsedData.Events.Where(e => e.GetKey() == key).ToList();
            
            foreach (var evt in eventsToRemove)
            {
                parsedData.Events.Remove(evt);
                eventFound = true;
                _log.Information("🗑️ Deleted event {Key}", key);
            }

            if (!eventFound)
            {
                _log.Warning("❌ Event with key {Key} not found", key);
                return new EventWriteResult(false, $"Event with key '{key}' not found");
            }

            // 4. Serialize back to YAML
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var yamlOutput = serializer.Serialize(new
            {
                events = parsedData.Events.Select(e => new
                {
                    date = e.Date.ToString("yyyy-MM-dd"),
                    time = e.Time?.ToString("HH:mm"),
                    subject = e.Subject,
                    description = e.Description
                }),
                shopping = parsedData.ShoppingItems.Select(s => s.Subject)
            });

            // 5. Update file on GitHub
            var updateResult = await _gitHubClient.UpdateFileContentAsync(yamlOutput, sha);
            
            return updateResult.Match(
                error =>
                {
                    _log.Error("❌ Failed to update GitHub: {Error}", error.Message);
                    return new EventWriteResult(false, error.Message);
                },
                _ =>
                {
                    _log.Information("✅ Successfully deleted event from GitHub");
                    return new EventWriteResult(true);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error deleting event");
            return new EventWriteResult(false, ex.Message);
        }
    }
}