using Reminder.Tests.EventProcessing.Helpers;
using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.Ntfy;
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
        var ntfyNotifier = new TestNtfyNotifier();
        
        var eventReader = new TestEventReader();
        eventReader.SetEvents(events);

        // Создаём процессоры
        var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, ntfyNotifier, NtfyTopics.DailyDigest);
        var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, ntfyNotifier, NtfyTopics.Reminders);
        var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, ntfyNotifier, NtfyTopics.WeeklyDigest);
        var twoWeekDigestProcessor = new TwoWeekDigestProcessor(dateTimeProvider, fileStorage, ntfyNotifier, NtfyTopics.TwoWeekDigest);
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
            twoWeekDigestProcessor,
            printer);

        // Act - вызываем все процессоры
        await dailyDigestProcessor.SendIfNeededAsync(events, friday);
        await reminderProcessor.SendIfNeededAsync(events, friday);
        
        // Для WeeklyDigestProcessor меняем время на 18:00 пятницы
        dateTimeProvider.SetNow(new DateTime(2026, 4, 10, 18, 0, 0));
        await weeklyDigestProcessor.SendIfNeededAsync(events, new DateTime(2026, 4, 10, 18, 0, 0));

        // Assert - все используют ntfyNotifier (3 сообщения: daily, reminder, weekly)
        Assert.Equal(3, ntfyNotifier.NotifiedMessages.Count);
        
        // Проверяем что есть напоминание (содержит "Через")
        Assert.Contains(ntfyNotifier.NotifiedMessages, m => m.Contains("Встреча через час"));
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

        var ntfyNotifier = new TestNtfyNotifier();
        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now);
        
        var processor = new ReminderProcessor(
            dateTimeProvider,
            new InMemoryFileStorage(),
            ntfyNotifier,
            NtfyTopics.Reminders);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.Single(ntfyNotifier.NotifiedMessages);
        Assert.Contains("Вовремя", ntfyNotifier.NotifiedMessages[0]);
        Assert.DoesNotContain("Слишком рано", ntfyNotifier.NotifiedMessages[0]);
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

        var ntfyNotifier = new TestNtfyNotifier();
        var dateTimeProvider = new MockDateTimeProvider();
        dateTimeProvider.SetNow(now);
        
        var processor = new DailyDigestProcessor(
            dateTimeProvider,
            new InMemoryFileStorage(),
            ntfyNotifier,
            NtfyTopics.DailyDigest);

        // Act
        await processor.SendIfNeededAsync(events, now);

        // Assert
        Assert.NotNull(ntfyNotifier.LastNotifiedMessage);
        Assert.Contains("Сегодня", ntfyNotifier.LastNotifiedMessage);
        Assert.DoesNotContain("Завтра", ntfyNotifier.LastNotifiedMessage);
        Assert.DoesNotContain("Вчера", ntfyNotifier.LastNotifiedMessage);
    }
}