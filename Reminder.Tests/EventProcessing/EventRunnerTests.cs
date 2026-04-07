using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Common;
using static Reminder.Tests.EventProcessing.Helpers.EventRunnerTestHelper;

namespace Reminder.Tests.EventProcessing;

public class EventRunnerTests
{
    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventDue_CallsNotifierAndSavesProcessed()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Date = DateOnly.FromDateTime(now),
            Time = new TimeOnly(now.Hour, now.Minute, now.Second),
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var notifier = new TestNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(notifier.LastNotifiedEvent);
        Assert.Equal(eventData.Subject, notifier.LastNotifiedEvent?.Subject);

        var savedProcessed = fileStorage.GetProcessed();
        var notifyKey = eventData.GetKey();
        Assert.True(savedProcessed.ContainsKey(notifyKey));
        Assert.Equal(now, savedProcessed[notifyKey]);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventInFuture_DoesNotCallNotifier()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var futureTime = now.AddSeconds(10);
        var eventData = new EventData
        {
            Date = DateOnly.FromDateTime(futureTime),
            Time = new TimeOnly(futureTime.Hour, futureTime.Minute, futureTime.Second),
            Subject = "Будущее событие",
            Description = "Описание"
        };

        var notifier = new TestNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Null(notifier.LastNotifiedEvent);
        Assert.Empty(notifier.NotifiedEvents);

        var savedProcessed = fileStorage.GetProcessed();
        var notifyKey = eventData.GetKey();
        Assert.False(savedProcessed.ContainsKey(notifyKey));
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventAlreadyProcessed_DoesNotCallNotifier()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Date = DateOnly.FromDateTime(now),
            Time = new TimeOnly(now.Hour, now.Minute, now.Second),
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var processed = new Dictionary<string, DateTime>
        {
            [eventData.GetKey()] = now
        };

        var notifier = new TestNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            processed: processed,
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Null(notifier.LastNotifiedEvent);
        Assert.Empty(notifier.NotifiedEvents);

        var savedProcessed = fileStorage.GetProcessed();
        var notifyKey = eventData.GetKey();
        Assert.True(savedProcessed.ContainsKey(notifyKey)); // ← уже было сохранено
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenNotifierThrows_DoesNotSaveProcessed()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Date = DateOnly.FromDateTime(now),
            Time = new TimeOnly(now.Hour, now.Minute, now.Second),
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var notifier = new ThrowingNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        var savedProcessed = fileStorage.GetProcessed();
        var notifyKey = eventData.GetKey();
        Assert.False(savedProcessed.ContainsKey(notifyKey));
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenMultipleEventsDue_AllAreNotified()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData1 = new EventData
        {
            Date = DateOnly.FromDateTime(now),
            Time = new TimeOnly(now.Hour, now.Minute, now.Second),
            Subject = "Событие 1",
            Description = "Описание 1"
        };

        var eventData2 = new EventData
        {
            Date = DateOnly.FromDateTime(now),
            Time = new TimeOnly(now.Hour, now.Minute, now.Second),
            Subject = "Событие 2",
            Description = "Описание 2"
        };

        var notifier = new TestNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData1, eventData2 },
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, notifier.NotifiedEvents.Count);
        Assert.Contains(notifier.NotifiedEvents, e => e.Subject == "Событие 1");
        Assert.Contains(notifier.NotifiedEvents, e => e.Subject == "Событие 2");

        var savedProcessed = fileStorage.GetProcessed();
        var notifyKey1 = eventData1.GetKey(); ;
        var notifyKey2 = eventData2.GetKey(); ;
        Assert.True(savedProcessed.ContainsKey(notifyKey1));
        Assert.True(savedProcessed.ContainsKey(notifyKey2));
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventReaderReturnsEmptyList_DoesNotCallNotifier()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var notifier = new TestNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData>(),
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Null(notifier.LastNotifiedEvent);
        Assert.Empty(notifier.NotifiedEvents);

        var savedProcessed = fileStorage.GetProcessed();
        Assert.Empty(savedProcessed);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventInPast_CallsNotifierAndSavesProcessed()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var pastTime = now.AddSeconds(-5);
        var eventData = new EventData
        {
            Date = DateOnly.FromDateTime(pastTime),
            Time = new TimeOnly(pastTime.Hour, pastTime.Minute, pastTime.Second),
            Subject = "Прошлое событие",
            Description = "Описание"
        };

        var notifier = new TestNotifier();
        var fileStorage = new InMemoryFileStorage();

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage);

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(notifier.LastNotifiedEvent);
        Assert.Equal(eventData.Subject, notifier.LastNotifiedEvent?.Subject);

        var savedProcessed = fileStorage.GetProcessed();
        var notifyKey = eventData.GetKey();
        Assert.True(savedProcessed.ContainsKey(notifyKey));
        Assert.Equal(now, savedProcessed[notifyKey]);
    }
}
