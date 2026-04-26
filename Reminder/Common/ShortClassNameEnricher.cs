using Serilog.Core;
using Serilog.Events;

namespace ReminderApp.Common;

public class ShortClassNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue) &&
            sourceContextValue is ScalarValue scalarValue &&
            scalarValue.Value is string fullName)
        {
            var shortName = fullName.Split(".").Last();
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClassName", shortName));
        }
    }
}
