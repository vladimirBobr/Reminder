using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class ReminderProcessor : IReminderProcessor
{
    private static readonly ILogger _log = Log.ForContext<ReminderProcessor>();
    private readonly int _remindMinutesBefore;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly INtfyNotifier _ntfy;

    // Кэш отправленных напоминаний (ключ = "yyyy-MM-dd HH:mm-Subject")
    private readonly HashSet<string> _sentReminders = new();
    private const string SentRemindersKey = "sent_reminders";

    public ReminderProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        INtfyNotifier ntfy,
        int remindMinutesBefore = 60)
    {
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _ntfy = ntfy;
        _remindMinutesBefore = remindMinutesBefore;

        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        await LoadSentRemindersAsync();
    }

    public async Task SendIfNeededAsync(List<EventData> events, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Фильтруем: события на сегодня, с указанием времени
        var todayEvents = events
            .Where(e => e.Date == today && e.Time.HasValue)
            .ToList();

        foreach (var evt in todayEvents)
        {
            var eventTime = evt.Date.ToDateTime(evt.Time!.Value);
            var minutesUntilEvent = (eventTime - now).TotalMinutes;

            // Отправляем если до события <= 60 минут (и оно ещё не началось)
            // Это покрывает: обычный случай (за час) и случай когда сервис лежал
            if (minutesUntilEvent > 0 && minutesUntilEvent <= _remindMinutesBefore)
            {
                var reminderKey = evt.GetKey();

                // Если ещё не отправляли
                if (!_sentReminders.Contains(reminderKey))
                {
                    await SendReminderAsync(evt, (int)minutesUntilEvent);
                    _sentReminders.Add(reminderKey);
                    await SaveSentRemindersAsync();
                }
            }
        }
    }

    private async Task SendReminderAsync(EventData evt, int minutesUntil)
    {
        var timeStr = evt.Time?.ToString("HH:mm") ?? "";
        var message = $"🔔 Через {minutesUntil} минут: {timeStr} - {evt.Subject}";

        if (!string.IsNullOrEmpty(evt.Description))
        {
            message += $"\n{evt.Description}";
        }

        await _ntfy.NotifyAsync(message);
        _log.Information("✅ Напоминание отправлено: {Subject}", evt.Subject);
    }

    private async Task LoadSentRemindersAsync()
    {
        var data = await _fileStorage.LoadStringAsync(SentRemindersKey);
        if (!string.IsNullOrEmpty(data))
        {
            var keys = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in keys)
            {
                _sentReminders.Add(key);
            }
        }
    }

    private async Task SaveSentRemindersAsync()
    {
        var data = string.Join(";", _sentReminders);
        await _fileStorage.SaveStringAsync(SentRemindersKey, data);
    }
}