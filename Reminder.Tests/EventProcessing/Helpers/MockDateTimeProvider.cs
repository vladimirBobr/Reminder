using ReminderApp.DateTimeProviding;

namespace Reminder.Tests.EventProcessing.Helpers;

public class MockDateTimeProvider : IDateTimeProvider
{
    public DateTime Now { get; set; } = DateTime.Now;

    public void SetNow(DateTime now)
    {
        Now = now;
    }
}
