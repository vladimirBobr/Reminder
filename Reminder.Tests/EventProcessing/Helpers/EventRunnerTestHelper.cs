using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing.Processors;

namespace Reminder.Tests.EventProcessing.Helpers;

public static class EventRunnerTestHelper
{
    public static DailyDigestProcessor CreateDailyDigestProcessor(
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

        return new DailyDigestProcessor(dateTimeProvider, fileStorage, new List<INotifier> { notifier });
    }

    public static ReminderProcessor CreateReminderProcessor(
        DateTime? now = null,
        List<EventData>? events = null,
        INtfyNotifier? notifier = null,
        InMemoryFileStorage? fileStorage = null)
    {
        now ??= DateTime.Now;
        notifier ??= new TestNtfyNotifier();
        fileStorage ??= new InMemoryFileStorage();

        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now.Value);

        return new ReminderProcessor(dateTimeProvider, fileStorage, notifier);
    }

    public static WeeklyDigestProcessor CreateWeeklyDigestProcessor(
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

        return new WeeklyDigestProcessor(dateTimeProvider, fileStorage, new List<INotifier> { notifier });
    }
}