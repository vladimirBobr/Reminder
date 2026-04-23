namespace ReminderApp.EventStorage;

public interface INotesService
{
    (string Error, string? Message) AddNote(string note, DateOnly? date = null);
}