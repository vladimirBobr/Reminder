using ReminderApp.EventParsing;
using ReminderApp.EventProcessing.Processors;

namespace Reminder.Tests.EventProcessing.Helpers;

public class TestShopListProcessor : IShopListProcessor
{
    public Task ProcessShoppingListAsync(List<ShoppingItem> shoppingItems, DateTime now)
    {
        // Test implementation - does nothing
        return Task.CompletedTask;
    }
}