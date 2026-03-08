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
            Time = now,
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
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.True(savedProcessed.ContainsKey(notifyKey));
        Assert.Equal(now, savedProcessed[notifyKey]);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventInFuture_DoesNotCallNotifier()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Time = now.AddSeconds(10),
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
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.False(savedProcessed.ContainsKey(notifyKey));
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventAlreadyProcessed_DoesNotCallNotifier()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Time = now,
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var processed = new Dictionary<string, DateTime>
        {
            [$"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}"] = now
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
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.True(savedProcessed.ContainsKey(notifyKey)); // ← уже было сохранено
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenNotifierThrows_DoesNotSaveProcessed()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Time = now,
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
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.False(savedProcessed.ContainsKey(notifyKey));
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenMultipleEventsDue_AllAreNotified()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData1 = new EventData
        {
            Time = now,
            Subject = "Событие 1",
            Description = "Описание 1"
        };

        var eventData2 = new EventData
        {
            Time = now,
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
        var notifyKey1 = $"notify-{eventData1.Time:yyyyMMddHHmmss}-{eventData1.Subject}";
        var notifyKey2 = $"notify-{eventData2.Time:yyyyMMddHHmmss}-{eventData2.Subject}";
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
        var eventData = new EventData
        {
            Time = now.AddSeconds(-5),
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
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.True(savedProcessed.ContainsKey(notifyKey));
        Assert.Equal(now, savedProcessed[notifyKey]);
    }
}
