using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing.Senders;

namespace Reminder.Tests.EventProcessing.Helpers;

public static class EventRunnerTestHelper
{
    public static DigestSender CreateDigestSender(
        DateTime? now = null,
        List<EventData>? events = null,
        INotifier? notifier = null,
        InMemoryFileStorage? fileStorage = null)
    {
        now ??= DateTime.Now;
        notifier ??= new TestNotifier();
        fileStorage ??= new InMemoryFileStorage();

        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now.Value);

        var eventReader = new TestEventReader();
        eventReader.SetEvents(events ?? new List<EventData>());

        return new DigestSender(dateTimeProvider, fileStorage, new List<INotifier> { notifier });
    }

    public static ReminderSender CreateReminderSender(
        DateTime? now = null,
        List<EventData>? events = null,
        INotifier? notifier = null,
        InMemoryFileStorage? fileStorage = null)
    {
        now ??= DateTime.Now;
        notifier ??= new TestNotifier();
        fileStorage ??= new InMemoryFileStorage();

        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now.Value);

        return new ReminderSender(dateTimeProvider, fileStorage, new List<INotifier> { notifier });
    }
}