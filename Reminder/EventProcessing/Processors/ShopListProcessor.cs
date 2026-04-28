using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventParsing;
using ReminderApp.FileStorage;

namespace ReminderApp.EventProcessing.Processors;

public class ShopListProcessor : IShopListProcessor
{
    private static readonly ILogger _log = Log.ForContext<ShopListProcessor>();
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;
    private readonly INtfyNotifier _ntfy;
    private readonly string _topic;

    private DateOnly? _lastSendDate7;
    private DateOnly? _lastSendDate18;
    private const string LastSendDate7Key = "last_shopping_send_date_7";
    private const string LastSendDate18Key = "last_shopping_send_date_18";

    public ShopListProcessor(
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage,
        INtfyNotifier ntfy,
        string topic = NtfyTopics.Shopping)
    {
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
        _ntfy = ntfy;
        _topic = topic;

        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        _lastSendDate7 = await LoadLastSendDateAsync(LastSendDate7Key);
        _lastSendDate18 = await LoadLastSendDateAsync(LastSendDate18Key);
    }

    public async Task ProcessShoppingListAsync(List<ShoppingItem> shoppingItems, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        // Send at 7:00
        if (now.Hour >= 7 && now.Hour < 8 && _lastSendDate7 != today)
        {
            await SendShoppingListAsync(shoppingItems, now, 7);
            _lastSendDate7 = today;
            await SaveLastSendDateAsync(LastSendDate7Key, today);
        }

        // Send at 18:00
        if (now.Hour >= 18 && now.Hour < 19 && _lastSendDate18 != today)
        {
            await SendShoppingListAsync(shoppingItems, now, 18);
            _lastSendDate18 = today;
            await SaveLastSendDateAsync(LastSendDate18Key, today);
        }
    }

    private async Task SendShoppingListAsync(List<ShoppingItem> shoppingItems, DateTime now, int hour)
    {
        if (shoppingItems.Count == 0)
        {
            _log.Information("🛒 Shopping list at {Hour}:00 - no items", hour);
            return;
        }

        _log.Information("🛒 Shopping list at {Hour}:00 - found {Count} items, sending...", hour, shoppingItems.Count);

        var message = BuildShoppingMessage(shoppingItems);
        await _ntfy.NotifyAsync(message, _topic);
        _log.Information("✅ Shopping list sent");
    }

    private string BuildShoppingMessage(List<ShoppingItem> items)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🛒 Shopping list ({items.Count} items):");
        sb.AppendLine();

        foreach (var item in items)
        {
            sb.AppendLine($"• {item.Subject}");
        }

        return sb.ToString();
    }

    private async Task<DateOnly?> LoadLastSendDateAsync(string key)
    {
        var data = await _fileStorage.LoadStringAsync(key);
        if (string.IsNullOrEmpty(data))
            return null;

        return DateOnly.Parse(data);
    }

    private async Task SaveLastSendDateAsync(string key, DateOnly date)
    {
        await _fileStorage.SaveStringAsync(key, date.ToString("yyyy-MM-dd"));
    }
}