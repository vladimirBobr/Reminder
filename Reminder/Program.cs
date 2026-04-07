using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventPrinter;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading;
using ReminderApp.EventScheduling;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    private static readonly IDateTimeProvider _dateTimeProvider = new DateTimeProvider();
    private static readonly IEventScheduler _eventScheduler = new EventScheduler();
    private static readonly IFileStorage _fileStorage = new JsonFileStorage();
    private static readonly GitHubConfigStorage _configStorage = new GitHubConfigStorage();
    private static readonly SmsAeroConfigStorage _smsAeroConfigStorage = new SmsAeroConfigStorage();

    static async Task Main(string[] args)
    {
        Console.WriteLine("▶️ Starting Reminder");

        var eventReader = new GitHubEventReaderFactory(_configStorage).Create();
        if (eventReader == null)
            return;

        // Get SMSAero credentials (ask from console or load from storage)
        var (email, apiToken, sign, phoneNumber) = GetSmsAeroCredentials();
        
        // Initialize SMSAero notifier
        var notifier = new SmsAeroNotifier(email, apiToken, sign);
        
        // Store phone number for later use in notifications
        if (!string.IsNullOrEmpty(phoneNumber))
        {
            Environment.SetEnvironmentVariable("SMSAERO_DEFAULT_PHONE", phoneNumber);
        }

        var eventPrinter = new EventPrinter.EventPrinter();

        var runner = new EventRunner(
            _eventScheduler,
            _dateTimeProvider,
            _fileStorage,
            eventReader,
            notifier,
            eventPrinter);

        await runner.StartAsync();

        Console.WriteLine("Нажмите любую клавишу для остановки...");
        Console.ReadKey();

        runner.Stop();
        Console.WriteLine("✅ Работа завершена.");
    }

    private static (string email, string apiToken, string sign, string? phoneNumber) GetSmsAeroCredentials()
    {
        var config = _smsAeroConfigStorage.Load();
        string email;
        string apiToken;
        string sign;
        string? phoneNumber;
        
        if (config != null && !string.IsNullOrEmpty(config.EncryptedToken))
        {
            var decryptedToken = _smsAeroConfigStorage.GetDecryptedToken(config.EncryptedToken);
            if (decryptedToken != null)
            {
                Console.WriteLine("✅ Загружены сохраненные SMSAero настройки");
                
                // Ask for phone number if not stored
                Console.Write("Введите номер телефона для SMS уведомлений (в формате 79000000000): ");
                phoneNumber = Console.ReadLine()?.Trim();
                
                return (config.Email, decryptedToken, config.Sign, phoneNumber);
            }
        }

        // Ask for credentials from console
        Console.WriteLine("Настройка SMSAero:");
        Console.Write("Email: ");
        email = Console.ReadLine()?.Trim() ?? "";
        
        Console.Write("API Token: ");
        apiToken = Console.ReadLine()?.Trim() ?? "";
        
        Console.Write("Подпись (по умолчанию SMS Aero): ");
        sign = Console.ReadLine()?.Trim() ?? "SMS Aero";
            
        Console.Write("Номер телефона для уведомлений (в формате 79000000000): ");
        phoneNumber = Console.ReadLine()?.Trim();

        // Save credentials (except phone number)
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(apiToken))
        {
            _smsAeroConfigStorage.Save(email, apiToken, sign);
            Console.WriteLine("✅ SMSAero настройки сохранены");
        }
        
        return (email, apiToken, sign, phoneNumber);
    }
}
