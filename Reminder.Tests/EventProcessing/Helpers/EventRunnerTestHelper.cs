using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;

namespace Reminder.Tests.EventProcessing.Helpers;

public static class EventRunnerTestHelper
{
    public static EventRunner CreateEventRunner(
        DateTime? now = null,
        List<EventData>? events = null,
        INotifier? notifier = null,
        InMemoryFileStorage? fileStorage = null)
    {
        now ??= DateTime.Now;
        events ??= new List<EventData>();
        notifier ??= new TestNotifier();
        fileStorage ??= new InMemoryFileStorage();

        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now.Value);

        var eventReader = new TestEventReader();
        eventReader.SetEvents(events);

        var printer = new EventOutputPrinter();

        var eventRunner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            eventReader,
            notifier,
            printer);

        return eventRunner;
    }
}