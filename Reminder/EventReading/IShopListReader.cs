using ReminderApp.EventParsing;

namespace ReminderApp.EventReading;

public interface IShopListReader
{
    Task<ShoppingSection?> ReadShoppingSectionAsync();
}