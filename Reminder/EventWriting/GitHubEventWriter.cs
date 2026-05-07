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

    public async Task<EventWriteResult> AddEventsAsync(List<EventData> newEvents)
    {
        if (newEvents == null || newEvents.Count == 0)
        {
            return new EventWriteResult(true); // Nothing to add
        }

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

            // 3. Add all new events
            foreach (var newEvent in newEvents)
            {
                parsedData.Events.Add(newEvent);
                _log.Information("➕ Added event: date={Date}, subject={Subject}, desc={Desc}",
                    newEvent.Date, newEvent.Subject, newEvent.Description);
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
                    _log.Information("✅ Successfully added {Count} events on GitHub", newEvents.Count);
                    return new EventWriteResult(true);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error adding events");
            return new EventWriteResult(false, ex.Message);
        }
    }

    public async Task<EventWriteResult> AddEventAsync(DateOnly date, string subject, string? description, TimeOnly? time = null)
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

            // 3. Create new event
            var newEvent = new EventData
            {
                Date = date,
                Time = time,
                Subject = subject,
                Description = description
            };
            
            parsedData.Events.Add(newEvent);
            var newKey = newEvent.GetKey();
            
            _log.Information("➕ Added new event {Key}: date={Date}, subject={Subject}, desc={Desc}, time={Time}",
                newKey, date, subject, description, time);

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
                    _log.Information("✅ Successfully added event on GitHub");
                    return new EventWriteResult(true, null, newKey);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error adding event");
            return new EventWriteResult(false, ex.Message);
        }
    }

    public async Task<EventWriteResult> UpdateEventAsync(string key, DateOnly? date, string? subject, string? description, TimeOnly? time = null)
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
                    if (date.HasValue) evt.Date = date.Value;
                    evt.Subject = subject;
                    evt.Description = description;
                    if (time.HasValue) evt.Time = time;
                    eventFound = true;
                    newKey = evt.GetKey();
                    _log.Information("✏️ Updated event {Key}: date={Date}, subject={Subject}, desc={Desc}, time={Time}, new key: {NewKey}", key, date, subject, description, time, newKey);
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

    public async Task<EventWriteResult> AddShoppingItemAsync(string item)
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

            // 3. Add new shopping item
            parsedData.ShoppingItems.Add(new ShoppingItem { Subject = item });
            
            _log.Information("➕ Added shopping item: {Item}", item);

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
                    _log.Information("✅ Successfully added shopping item on GitHub");
                    return new EventWriteResult(true);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error adding shopping item");
            return new EventWriteResult(false, ex.Message);
        }
    }

    public async Task<EventWriteResult> DeleteShoppingItemAsync(string item)
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

            // 3. Find and remove shopping item
            var itemFound = false;
            var itemsToRemove = parsedData.ShoppingItems.Where(s => s.Subject == item).ToList();
            
            foreach (var shoppingItem in itemsToRemove)
            {
                parsedData.ShoppingItems.Remove(shoppingItem);
                itemFound = true;
                _log.Information("🗑️ Deleted shopping item: {Item}", item);
            }

            if (!itemFound)
            {
                _log.Warning("❌ Shopping item not found: {Item}", item);
                return new EventWriteResult(false, $"Shopping item not found");
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
                    _log.Information("✅ Successfully deleted shopping item from GitHub");
                    return new EventWriteResult(true);
                }
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "❌ Error deleting shopping item");
            return new EventWriteResult(false, ex.Message);
        }
    }
}
