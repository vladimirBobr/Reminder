namespace ReminderApp.EventProcessing;

public interface IEventRunner
{
    Task StartAsync();
    void Stop();
}
