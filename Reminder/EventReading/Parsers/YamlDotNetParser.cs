using ReminderApp.Common;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReminderApp.EventReading.Parsers;

/// <summary>
/// YAML file structure matching the expected format
/// </summary>
internal class YamlFileData
{
    public List<YamlEventData> Events { get; set; } = [];
    public List<string> Shopping { get; set; } = [];
}

/// <summary>
/// YAML event data structure
/// </summary>
internal class YamlEventData
{
    public string Date { get; set; } = string.Empty;
    public string? Time { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// YAML parser implementation using YamlDotNet
/// </summary>
public class YamlDotNetParser : IYamlParser
{
    private readonly IDeserializer _deserializer;

    public YamlDotNetParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public ParsedFileData Parse(string yaml)
    {
        var yamlData = _deserializer.Deserialize<YamlFileData>(yaml);

        var events = yamlData.Events.Select(e => new EventData
        {
            Date = DateOnly.Parse(e.Date),
            Time = string.IsNullOrEmpty(e.Time) ? null : TimeOnly.Parse(e.Time),
            Subject = e.Subject ?? string.Empty,
            Description = e.Description
        }).ToList();

        var shoppingItems = yamlData.Shopping.Select(s => new ShoppingItem
        {
            Subject = s
        }).ToList();

        return new ParsedFileData
        {
            Events = events,
            ShoppingItems = shoppingItems
        };
    }
}