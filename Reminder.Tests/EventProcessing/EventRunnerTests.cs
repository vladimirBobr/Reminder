using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Events;
using static Reminder.Tests.EventProcessing.Helpers.EventRunnerTestHelper;

namespace Reminder.Tests.EventProcessing;

public class EventRunnerTests
{
    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventDue_CallsNotifier()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Time = now,
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var notifier = new TestNotifier(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(notifier.LastNotifiedEvent);
        Assert.Equal(eventData.Subject, notifier.LastNotifiedEvent?.Subject);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventInFuture_DoesNotCallNotifier()
    {
        // Arrange
        var now = DateTime.Now;
        var eventData = new EventData
        {
            Time = now.AddSeconds(10),
            Subject = "Будущее событие",
            Description = "Описание"
        };

        var notifier = new TestNotifier(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Null(notifier.LastNotifiedEvent);
        Assert.Empty(notifier.NotifiedEvents);
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

        var notifier = new TestNotifier(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            processed: processed,
            notifier: notifier); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Null(notifier.LastNotifiedEvent);
        Assert.Empty(notifier.NotifiedEvents);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventDue_SavesProcessed()
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
        var fileStorage = new InMemoryFileStorage(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        var savedProcessed = fileStorage.GetProcessed(); // ← проверяем состояние
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.True(savedProcessed.ContainsKey(notifyKey));
        Assert.Equal(now, savedProcessed[notifyKey]);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenNotifierThrows_ExceptionIsHandled()
    {
        // Arrange
        var now = DateTime.Now;
        var eventData = new EventData
        {
            Time = now,
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var notifier = new ThrowingNotifier(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        // Проверяем, что метод не упал — тест прошёл
        // Дополнительно можно проверить, что в консоль вывелось сообщение об ошибке
        // Но в данном случае — достаточно, что тест не упал
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventDue_SetsProcessedWithCorrectTime()
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
        var fileStorage = new InMemoryFileStorage(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        var savedProcessed = fileStorage.GetProcessed(); // ← проверяем состояние
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.True(savedProcessed.ContainsKey(notifyKey));
        Assert.Equal(now, savedProcessed[notifyKey]);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenEventDueButNotifierFails_DoesNotSaveProcessed()
    {
        // Arrange
        var now = DateTime.Now.RoundToStartOfMinute();
        var eventData = new EventData
        {
            Time = now,
            Subject = "Тестовое событие",
            Description = "Описание"
        };

        var notifier = new ThrowingNotifier(); // ← передаём по ссылке
        var fileStorage = new InMemoryFileStorage(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData },
            notifier: notifier,
            fileStorage: fileStorage); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        var savedProcessed = fileStorage.GetProcessed(); // ← проверяем состояние
        var notifyKey = $"notify-{eventData.Time:yyyyMMddHHmmss}-{eventData.Subject}";
        Assert.False(savedProcessed.ContainsKey(notifyKey));
    }

    [Fact]
    public async Task CheckAndNotifyAsync_WhenMultipleEventsDue_AllAreNotified()
    {
        // Arrange
        var now = DateTime.Now;
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

        var notifier = new TestNotifier(); // ← передаём по ссылке

        var eventRunner = CreateEventRunner(
            now: now,
            events: new List<EventData> { eventData1, eventData2 },
            notifier: notifier); // ← передаём

        // Act
        await eventRunner.CheckAndNotifyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, notifier.NotifiedEvents.Count);
        Assert.Contains(notifier.NotifiedEvents, e => e.Subject == "Событие 1");
        Assert.Contains(notifier.NotifiedEvents, e => e.Subject == "Событие 2");
    }
}
