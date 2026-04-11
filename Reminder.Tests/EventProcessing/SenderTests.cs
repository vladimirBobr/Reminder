using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Common;
using ReminderApp.EventProcessing.Senders;
using static Reminder.Tests.EventProcessing.Helpers.EventRunnerTestHelper;

namespace Reminder.Tests.EventProcessing;

public class DigestSenderTests
{
    [Fact]
    public async Task SendIfNeededAsync_WhenNot7AM_DoesNotSend()
    {
        // Arrange - 6 утра
        var now = new DateTime(2026, 4, 11, 6, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие" }
        };

        var notifier = new TestNotifier();
        var sender = CreateDigestSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_WhenAfter7AM_SendsDigest()
    {
        // Arrange - 7:30 утра
        var now = new DateTime(2026, 4, 11, 7, 30, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие 1" },
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие 2" }
        };

        var notifier = new TestNotifier();
        var sender = CreateDigestSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Событие 1", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_IgnoresFutureEvents()
    {
        // Arrange - 7:00 утра
        var now = new DateTime(2026, 4, 11, 7, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Сегодня" },
            new() { Date = new DateOnly(2026, 4, 12), Subject = "Завтра" }
        };

        var notifier = new TestNotifier();
        var sender = CreateDigestSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        var message = notifier.LastNotifiedMessage!;
        Assert.Contains("Сегодня", message);
        Assert.DoesNotContain("Завтра", message);
    }
}

public class ReminderSenderTests
{
    [Fact]
    public async Task SendIfNeededAsync_WhenNoTime_DoesNotSend()
    {
        // Arrange
        var now = new DateTime(2026, 4, 11, 10, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 11), Subject = "Событие без времени" } // нет Time
        };

        var notifier = new TestNotifier();
        var sender = CreateReminderSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_WhenOneHourBefore_SendsReminder()
    {
        // Arrange - 10:00, событие в 11:00 (через час)
        var now = new DateTime(2026, 4, 11, 10, 0, 0);
        
        var events = new List<EventData>
        {
            new()
            {
                Date = new DateOnly(2026, 4, 11),
                Time = new TimeOnly(11, 0),
                Subject = "Встреча"
            }
        };

        var notifier = new TestNotifier();
        var sender = CreateReminderSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Встреча", notifier.LastNotifiedMessage);
        Assert.Contains("60 минут", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_When45MinutesBefore_SendsReminder()
    {
        // Arrange - 10:15, событие в 11:00 (через 45 минут)
        var now = new DateTime(2026, 4, 11, 10, 15, 0);
        
        var events = new List<EventData>
        {
            new()
            {
                Date = new DateOnly(2026, 4, 11),
                Time = new TimeOnly(11, 0),
                Subject = "Встреча"
            }
        };

        var notifier = new TestNotifier();
        var sender = CreateReminderSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_WhenTooEarly_DoesNotSend()
    {
        // Arrange - 9:00, событие в 11:00 (через 2 часа - рано)
        var now = new DateTime(2026, 4, 11, 9, 0, 0);
        
        var events = new List<EventData>
        {
            new()
            {
                Date = new DateOnly(2026, 4, 11),
                Time = new TimeOnly(11, 0),
                Subject = "Встреча"
            }
        };

        var notifier = new TestNotifier();
        var sender = CreateReminderSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_WhenServiceWasDown_SendsReminder()
    {
        // Arrange - 10:59, событие в 11:00 (через 1 минуту - сервис лежал)
        var now = new DateTime(2026, 4, 11, 10, 59, 0);
        
        var events = new List<EventData>
        {
            new()
            {
                Date = new DateOnly(2026, 4, 11),
                Time = new TimeOnly(11, 0),
                Subject = "Срочная встреча"
            }
        };

        var notifier = new TestNotifier();
        var sender = CreateReminderSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert - должно отправить, т.к. сервис был недоступен
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Срочная встреча", notifier.LastNotifiedMessage);
        Assert.Contains("1 минут", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_WhenEventAlreadyStarted_DoesNotSend()
    {
        // Arrange - 11:01, событие в 11:00 (уже началось)
        var now = new DateTime(2026, 4, 11, 11, 1, 0);
        
        var events = new List<EventData>
        {
            new()
            {
                Date = new DateOnly(2026, 4, 11),
                Time = new TimeOnly(11, 0),
                Subject = "Встреча"
            }
        };

        var notifier = new TestNotifier();
        var sender = CreateReminderSender(now: now, events: events, notifier: notifier);
        await sender.InitializeAsync();

        // Act
        await sender.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }
}