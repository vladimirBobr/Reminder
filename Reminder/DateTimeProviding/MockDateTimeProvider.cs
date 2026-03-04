namespace ReminderApp.DateTimeProviding;

public class MockDateTimeProvider : IDateTimeProvider
{
    public DateTime Now { get; set; } = DateTime.Now;

    public void SetNow(DateTime now)
    {
        Now = now;
    }
}
