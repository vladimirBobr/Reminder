using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Senders;

public class ReminderSender : IReminderSender
{
    private readonly IFileStorage _fileStorage;
    private readonly IEnumerable<INotifier> _notifiers;
    private readonly int _remindMinutesBefore;

    // Кэш отправленных напоминаний (ключ = "yyyy-MM-dd HH:mm-Subject")
    private readonly HashSet<string> _sentReminders = new();
    private const string SentRemindersKey = "sent_reminders";

    public ReminderSender(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        IEnumerable<INotifier> notifiers,
        int remindMinutesBefore = 60)
    {
        _fileStorage = fileStorage;
        _notifiers = notifiers ?? throw new ArgumentNullException(nameof(notifiers));
        _remindMinutesBefore = remindMinutesBefore;
    }

    /// <summary>
    /// Загружает отправленные напоминания (вызывается при старте)
    /// </summary>
    public async Task InitializeAsync()
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

        foreach (var notifier in _notifiers)
        {
            notifier.Notify(message);
        }
        Log.Information($"✅ Напоминание отправлено: {evt.Subject}");
    }

    private async Task LoadSentRemindersAsync()
    {
        try
        {
            var data = await _fileStorage.LoadAsync(SentRemindersKey);
            if (!string.IsNullOrEmpty(data))
            {
                var keys = data.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var key in keys)
                {
                    _sentReminders.Add(key);
                }
            }
            Log.Information($"📋 Загружено {_sentReminders.Count} напоминаний");
        }
        catch
        {
            // Игнорируем ошибки
        }
    }

    private async Task SaveSentRemindersAsync()
    {
        try
        {
            var data = string.Join(";", _sentReminders);
            await _fileStorage.SaveAsync(SentRemindersKey, data);
        }
        catch (Exception ex)
        {
            Log.Information($"❌ Не удалось сохранить напоминания: {ex.Message}");
        }
    }
}
