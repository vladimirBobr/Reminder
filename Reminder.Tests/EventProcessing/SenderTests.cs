using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Common;
using ReminderApp.EventProcessing.Processors;
using static Reminder.Tests.EventProcessing.Helpers.EventRunnerTestHelper;

namespace Reminder.Tests.EventProcessing;

public class DailyDigestProcessorTests
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
        var processor = CreateDailyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateDailyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateDailyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        var message = notifier.LastNotifiedMessage!;
        Assert.Contains("Сегодня", message);
        Assert.DoesNotContain("Завтра", message);
    }
}

public class ReminderProcessorTests
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
        var processor = CreateReminderProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateReminderProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateReminderProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateReminderProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateReminderProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

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
        var processor = CreateReminderProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }
}

public class WeeklyDigestProcessorTests
{
    [Fact]
    public async Task SendIfNeededAsync_WhenNotScheduledTime_DoesNotSend()
    {
        // Arrange - суббота 10:00 (не в расписании)
        var now = new DateTime(2026, 4, 11, 10, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 13), Subject = "Понедельник" }
        };

        var notifier = new TestNotifier();
        var processor = CreateWeeklyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_OnFriday18_SendsNextWeekEvents()
    {
        // Arrange - пятница 18:00
        var now = new DateTime(2026, 4, 10, 18, 0, 0);
        
        // Событие на следующий понедельник (13.04)
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 13), Subject = "Встреча в понедельник" },
            new() { Date = new DateOnly(2026, 4, 14), Subject = "Встреча во вторник" }
        };

        var notifier = new TestNotifier();
        var processor = CreateWeeklyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Встреча в понедельник", notifier.LastNotifiedMessage);
        Assert.Contains("Встреча во вторник", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_OnSunday20_SendsNextWeekEvents()
    {
        // Arrange - воскресенье 20:00
        var now = new DateTime(2026, 4, 12, 20, 0, 0);
        
        // Событие на следующий понедельник (13.04)
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 13), Subject = "Встреча в понедельник" }
        };

        var notifier = new TestNotifier();
        var processor = CreateWeeklyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Встреча в понедельник", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_IncludesWeekendDays()
    {
        // Arrange - пятница 18:00
        var now = new DateTime(2026, 4, 10, 18, 0, 0);
        
        // События на всю следующую неделю включая выходные
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 13), Subject = "Понедельник" },
            new() { Date = new DateOnly(2026, 4, 17), Subject = "Пятница" },
            new() { Date = new DateOnly(2026, 4, 18), Subject = "Суббота" },
            new() { Date = new DateOnly(2026, 4, 19), Subject = "Воскресенье" }
        };

        var notifier = new TestNotifier();
        var processor = CreateWeeklyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Понедельник", notifier.LastNotifiedMessage);
        Assert.Contains("Пятница", notifier.LastNotifiedMessage);
        Assert.Contains("Суббота", notifier.LastNotifiedMessage);
        Assert.Contains("Воскресенье", notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_WhenNoEventsOnNextWeek_DoesNotSend()
    {
        // Arrange - пятница 18:00
        var now = new DateTime(2026, 4, 10, 18, 0, 0);
        
        // Нет событий на следующую неделю
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 10), Subject = "Сегодня" }  // только сегодня
        };

        var notifier = new TestNotifier();
        var processor = CreateWeeklyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.Null(notifier.LastNotifiedMessage);
    }

    [Fact]
    public async Task SendIfNeededAsync_CallTwice_SendsBothTimes()
    {
        // Arrange - пятница 18:00
        var now = new DateTime(2026, 4, 10, 18, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = new DateOnly(2026, 4, 13), Subject = "Встреча" }
        };

        var notifier = new TestNotifier();
        var processor = CreateWeeklyDigestProcessor(now: now, events: events, notifier: notifier);

        // Act - первый вызов (пятница)
        await processor.SendIfNeededAsync(events, now);

        // Assert - первый раз отправилось
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Equal(1, notifier.NotifiedMessages.Count);

        // Меняем время на воскресенье 20:00
        var sunday = new DateTime(2026, 4, 12, 20, 0, 0);
        
        // Act - второй вызов (воскресенье)
        await processor.SendIfNeededAsync(events, sunday);

        // Assert - должно отправиться второй раз
        Assert.Equal(2, notifier.NotifiedMessages.Count);
    }
}