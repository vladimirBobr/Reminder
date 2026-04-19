using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public abstract class ProcessorBase : IProcessor
{
    protected readonly IFileStorage _fileStorage;
    protected readonly IEnumerable<INotifier> _notifiers;
    protected readonly IDateTimeProvider _dateTimeProvider;

    protected ProcessorBase(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEnumerable<INotifier> notifiers)
    {
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _notifiers = notifiers ?? throw new ArgumentNullException(nameof(notifiers));

        // Запускаем асинхронную инициализацию синхронно
        InitializeCoreAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeCoreAsync()
    {
        await OnInitializeAsync();
    }

    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    public abstract Task SendIfNeededAsync(List<EventData> events, DateTime now);

    protected async Task NotifyAllAsync(string message)
    {
        foreach (var notifier in _notifiers)
        {
            await notifier.NotifyAsync(message);
        }
    }

    protected async Task<string?> LoadAsync(string key)
    {
        try
        {
            return await _fileStorage.LoadAsync(key);
        }
        catch
        {
            return null;
        }
    }

    protected async Task SaveAsync(string key, string data)
    {
        try
        {
            await _fileStorage.SaveAsync(key, data);
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Не удалось сохранить данные ({key}): {ex.Message}");
        }
    }
}

public interface IProcessor
{
    Task SendIfNeededAsync(List<EventData> events, DateTime now);
}