using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventProcessing.Processors;

namespace Reminder.Tests.EventProcessing.Helpers;

public static class EventRunnerTestHelper
{
    public static DailyDigestProcessor CreateDailyDigestProcessor(
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

        return new DailyDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.DailyDigest);
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

        return new ReminderProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.Reminders);
    }

    public static WeeklyDigestProcessor CreateWeeklyDigestProcessor(
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

        return new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.WeeklyDigest);
    }
}