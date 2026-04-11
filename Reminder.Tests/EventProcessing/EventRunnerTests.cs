using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Common;
using static Reminder.Tests.EventProcessing.Helpers.EventRunnerTestHelper;

namespace Reminder.Tests.EventProcessing;

public class EventRunnerTests
{
    [Fact]
    public async Task CheckAndSendDigestIfNeededAsync_WhenNot7AM_DoesNotSend()
    {
        // Arrange - 6 утра
        var now = new DateTime(2026, 4, 11, 6, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие" }
        };

        var notifier = new TestNotifier();

        var eventRunner = CreateEventRunner(
            now: now,
            events: events,
            notifier: notifier);

        // Act
        await eventRunner.CheckAndSendDigestIfNeededAsync();

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task CheckAndSendDigestIfNeededAsync_WhenAfter7AM_SendsDigest()
    {
        // Arrange - 7:30 утра (после 7:00)
        var now = new DateTime(2026, 4, 11, 7, 30, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие 1" },
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие 2" }
        };

        var notifier = new TestNotifier();

        var eventRunner = CreateEventRunner(
            now: now,
            events: events,
            notifier: notifier);

        // Act
        await eventRunner.CheckAndSendDigestIfNeededAsync();

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Событие 1", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task CheckAndSendDigestIfNeededAsync_At8AM_SendsDigest()
    {
        // Arrange - 8:00 утра
        var now = new DateTime(2026, 4, 11, 8, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие" }
        };

        var notifier = new TestNotifier();

        var eventRunner = CreateEventRunner(
            now: now,
            events: events,
            notifier: notifier);

        // Act
        await eventRunner.CheckAndSendDigestIfNeededAsync();

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task CheckAndSendDigestIfNeededAsync_IgnoresFutureEvents()
    {
        // Arrange - 7:00 утра
        var now = new DateTime(2026, 4, 11, 7, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Сегодня" },
            new() { Date = new DateOnly(2026, 4, 12), Subject = "Завтра" }
        };

        var notifier = new TestNotifier();

        var eventRunner = CreateEventRunner(
            now: now,
            events: events,
            notifier: notifier);

        // Act
        await eventRunner.CheckAndSendDigestIfNeededAsync();

        // Assert
        var message = notifier.LastNotifiedMessage!;
        Assert.Contains("Сегодня", message);
        Assert.DoesNotContain("Завтра", message);
    }

    [Fact]
    public async Task CheckAndSendDigestIfNeededAsync_NoEventsForToday_DoesNotSend()
    {
        // Arrange - 7:00 утра
        var now = new DateTime(2026, 4, 11, 7, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 12), Subject = "Завтра" }
        };

        var notifier = new TestNotifier();

        var eventRunner = CreateEventRunner(
            now: now,
            events: events,
            notifier: notifier);

        // Act
        await eventRunner.CheckAndSendDigestIfNeededAsync();

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendDailyDigestAsync_WithTimeAndDescription_FormatsCorrectly()
    {
        // Arrange
        var now = new DateTime(2026, 4, 11, 7, 0, 0);
        
        var events = new List<EventData>
        {
            new() 
            { 
                Date = new DateOnly(2026, 4, 11), 
                Time = new TimeOnly(10, 30),
                Subject = "Встреча",
                Description = "Обсудить проект"
            }
        };

        var notifier = new TestNotifier();

        var eventRunner = CreateEventRunner(
            now: now,
            events: events,
            notifier: notifier);

        // Act
        await eventRunner.SendDailyDigestAsync(now);

        // Assert
        var message = notifier.LastNotifiedMessage!;
        Assert.Contains("10:30", message);
        Assert.Contains("Встреча", message);
        Assert.Contains("Обсудить проект", message);
    }
}