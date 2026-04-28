namespace ReminderApp.EventStorage;

public interface IShopListService
{
    (string Error, string? Message) AddItem(string item);
}