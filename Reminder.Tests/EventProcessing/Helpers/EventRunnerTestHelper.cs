using ReminderApp.EventNotification;
using ReminderApp.EventProcessing;
using ReminderApp.Events;
using ReminderApp.EventScheduling;
using ReminderApp.FileStorage;

namespace Reminder.Tests.EventProcessing.Helpers;

public static class EventRunnerTestHelper
{
    public static EventRunner CreateEventRunner(
        DateTime now = default,
        List<EventData>? events = null,
        Dictionary<string, DateTime>? processed = null,
        INotifier? notifier = null,
        IFileStorage? fileStorage = null,
        IEventScheduler? scheduler = null)
    {
        // Устанавливаем значения по умолчанию
        now = now == default ? DateTime.Now : now;
        events = events ?? new List<EventData>();
        processed = processed ?? new Dictionary<string, DateTime>();
        notifier = notifier ?? new TestNotifier();
        fileStorage = fileStorage ?? new InMemoryFileStorage();
        scheduler = scheduler ?? new EventScheduler();

        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now);

        var eventReader = new TestEventReader();
        eventReader.SetEvents(events);

        // Устанавливаем processed в fileStorage
        (fileStorage as InMemoryFileStorage)?.SetProcessed(processed);

        var eventRunner = new EventRunner(
            scheduler,
            dateTimeProvider,
            fileStorage,
            eventReader,
            notifier);

        return eventRunner;
    }
}
