using ReminderApp.Common;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.Telegram;

namespace Reminder.Tests._ForDebug;


public class TelegramNotificationDebug
{
    //[Fact(Skip ="для дебага")]
    public void Notify_ShouldSendMessageToTelegram()
    {
        // Arrange
        var notifier = new TelegramNotifier();

        var eventData = new EventData
        {
            Date = new DateOnly(2026, 3, 24),
            Time = new TimeOnly(22, 0, 0),
            Subject = "Тестовое событие",
            Description = "Это тестовое описание для отладки Telegram."
        };

        // Act
        notifier.Notify(eventData);

        // Assert
        // Мы не можем проверить, пришло ли сообщение в Telegram — но мы можем проверить,
        // что в консоль вывелся лог об успешной отправке.
        // Для этого — просто запусти тест и посмотри в консоль.
        // Если видишь: "✅ Уведомление отправлено в Telegram: Тестовое событие" — значит, всё работает.
    }
}
