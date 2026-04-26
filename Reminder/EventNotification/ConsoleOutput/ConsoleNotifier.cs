using ReminderApp.EventNotification.Ntfy;

namespace ReminderApp.EventNotification.ConsoleOutput;

public class ConsoleNotifier : INtfyNotifier
{
    private static readonly ILogger _log = Log.ForContext<ConsoleNotifier>();

    public Task NotifyAsync(string message)
    {
        _log.Information("🔔 УВЕДОМЛЕНИЕ:\n{Msg}\n---", message);
        return Task.CompletedTask;
    }
}
