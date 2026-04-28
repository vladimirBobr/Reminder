using ReminderApp.EventParsing;

namespace ReminderApp.EventProcessing.Processors;

public interface IShopListProcessor
{
    Task ProcessShoppingListAsync(List<ShoppingItem> shoppingItems, DateTime now);
}