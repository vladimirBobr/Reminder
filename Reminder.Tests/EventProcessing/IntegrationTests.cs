using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Processors;
using ReminderApp.FileStorage;
using MockDateTimeProvider = ReminderApp.DateTimeProviding.MockDateTimeProvider;

namespace Reminder.Tests.EventProcessing;

/// <summary>
/// Интеграционный тест - проверяет что все три процессора работают вместе
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task AllProcessors_SendCorrectNotifications()
    {
        // Arrange - пятница 10:00 (утро до всех событий)
        // 1. Сегодня в 11:00 - ReminderProcessor (через час)
        // 2. Сегодня - DailyDigestProcessor (после 7:00)
        // 3. На следующую неделю - WeeklyDigestProcessor (пятница 18:00 сработает)
        
        var friday = new DateTime(2026, 4, 10, 10, 0, 0);
        
        // События
        var events = new List<EventData>
        {
            // Сегодня 11:00 - для ReminderProcessor (через час)
            new()
            {
                Date = DateOnly.FromDateTime(friday),
                Time = new TimeOnly(11, 0),
                Subject = "Встреча через час",
                Description = "Должна прийти как напоминание"
            },
            // Сегодня - для DailyDigestProcessor
            new()
            {
                Date = DateOnly.FromDateTime(friday),
                Subject = "Ежедневное событие",
                Description = "Должно быть в ежедневном дайджесте"
            },
            // Следующая неделя (пн) - для WeeklyDigestProcessor
            new()
            {
                Date = new DateOnly(2026, 4, 13),
                Subject = "Встреча на следующей неделе",
                Description = "Должна быть в еженедельном дайджесте"
            }
        };

        // Создаём моки
        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(friday);
        
        var fileStorage = new InMemoryFileStorage();
        var notifier = new TestNotifier();
        var notifiers = new List<INotifier> { notifier };
        
        var eventReader = new TestEventReader();
        eventReader.SetEvents(events);

        // Создаём процессоры
        var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, notifiers);
        var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var currentWeekDigestProcessor = new CurrentWeekDigestProcessor(dateTimeProvider, fileStorage, notifiers);
        var printer = new EventOutputPrinter(dateTimeProvider);

        // Создаём EventRunner
        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            eventReader,
            printer,
            dailyDigestProcessor,
            reminderProcessor,
            weeklyDigestProcessor,
            currentWeekDigestProcessor,
            printer);

        // Act - вызываем все процессоры
        await dailyDigestProcessor.SendIfNeededAsync(events, friday);
        await reminderProcessor.SendIfNeededAsync(events, friday);
        
        // Для WeeklyDigestProcessor меняем время на 18:00 пятницы
        dateTimeProvider.SetNow(new DateTime(2026, 4, 10, 18, 0, 0));
        await weeklyDigestProcessor.SendIfNeededAsync(events, new DateTime(2026, 4, 10, 18, 0, 0));

        // Assert - проверяем что все три отправлены
        Assert.Equal(3, notifier.NotifiedMessages.Count);
        
        // Проверяем что есть напоминание (содержит "Через")
        Assert.Contains(notifier.NotifiedMessages, m => m.Contains("Встреча через час"));
        
        // Проверяем что есть ежедневный дайджест (содержит "Ежедневное")
        Assert.Contains(notifier.NotifiedMessages, m => m.Contains("Ежедневное событие"));
        
        // Проверяем что есть еженедельный дайджест (содержит "Следующей")
        Assert.Contains(notifier.NotifiedMessages, m => m.Contains("Встреча на следующей неделе"));
    }

    [Fact]
    public async Task ReminderProcessor_OnlySendsWithinOneHour()
    {
        // Arrange
        var now = new DateTime(2026, 4, 10, 10, 0, 0);
        
        var events = new List<EventData>
        {
            new()
            {
                Date = DateOnly.FromDateTime(now.AddHours(2)), // через 2 часа - не должно
                Time = new TimeOnly(12, 0),
                Subject = "Слишком рано"
            },
            new()
            {
                Date = DateOnly.FromDateTime(now.AddMinutes(30)), // через 30 минут - должно
                Time = new TimeOnly(10, 30),
                Subject = "Вовремя"
            }
        };

        var notifier = new TestNotifier();
        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now);
        
        var processor = new ReminderProcessor(
            dateTimeProvider,
            new InMemoryFileStorage(),
            new List<INotifier> { notifier });

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.Single(notifier.NotifiedMessages);
        Assert.Contains("Вовремя", notifier.NotifiedMessages[0]);
        Assert.DoesNotContain("Слишком рано", notifier.NotifiedMessages[0]);
    }

    [Fact]
    public async Task DailyDigestProcessor_OnlyTodayEvents()
    {
        // Arrange - 8 утра
        var now = new DateTime(2026, 4, 10, 8, 0, 0);
        
        var events = new List<EventData>
        {
            new() { Date = DateOnly.FromDateTime(now), Subject = "Сегодня" },
            new() { Date = DateOnly.FromDateTime(now.AddDays(1)), Subject = "Завтра - не должно" },
            new() { Date = DateOnly.FromDateTime(now.AddDays(-1)), Subject = "Вчера - не должно" }
        };

        var notifier = new TestNotifier();
        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now);
        
        var processor = new DailyDigestProcessor(
            dateTimeProvider,
            new InMemoryFileStorage(),
            new List<INotifier> { notifier });

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(notifier.LastNotifiedMessage);
        Assert.Contains("Сегодня", notifier.LastNotifiedMessage);
        Assert.DoesNotContain("Завтра", notifier.LastNotifiedMessage);
        Assert.DoesNotContain("Вчера", notifier.LastNotifiedMessage);
    }
}