namespace ReminderApp.EventReading.Parsers;

/// <summary>
/// Interface for YAML parsing operations
/// </summary>
public interface IYamlParser
{
    /// <summary>
    /// Parse YAML content into ParsedFileData
    /// </summary>
    ParsedFileData Parse(string yaml);
}