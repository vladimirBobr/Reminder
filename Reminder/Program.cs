using ReminderApp;
using ReminderApp.Authentication;
using ReminderApp.Common;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification.ConsoleOutput;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Processors;
using ReminderApp.EventReading;
using ReminderApp.EventReading.Parsers;
using ReminderApp.FileStorage;
using ReminderApp.GitHubApi;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();

// Add Admin Authentication (Cookie + Bearer)
builder.Services.AddAdminAuthentication();

// В DEBUG режиме отключаем шумные ASP.NET Core логи (идут через Microsoft.Extensions.Logging, не через Serilog)
if (DebugHelper.IsDebug)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.WebTools", LogLevel.Warning);
}

// Configure logging before anything else
var log = ConfigureLogger();
log.Information("▶ Starting Reminder");

try
{
    var (app, runner) = InitializeApp(builder, log);
    
    _ = runner.StartAsync();
    app.Run();
}
catch (Exception ex)
{
    log.Fatal(ex, "❌ Application crashed. Error: {ErrorMessage}", ex.Message);
    throw;
}

static (WebApplication app, EventRunner runner) InitializeApp(WebApplicationBuilder builder, ILogger log)
{
    var dateTimeProvider = new DateTimeProvider();

    // В DEBUG режиме храним данные выше проекта, чтобы не попадать в GIT
    string? debugDataPath = null;
    if (DebugHelper.IsDebug)
    {
        // Поднимаемся на 5 уровней вверх
        debugDataPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "debug_data");
        Directory.CreateDirectory(debugDataPath);
        log.Information("DEBUG MODE: данные хранятся в {Path}", debugDataPath);
    }

    var fileStorage = new JsonFileStorage(debugDataPath);

    // В DEBUG режиме используем заглушку для консоли, в RELEASE - Ntfy
    INtfyNotifier notifier;
    if (DebugHelper.IsDebug)
    {
        notifier = new ConsoleNotifier();
        log.Information("DEBUG MODE: используется ConsoleNotifier (без отправки в Ntfy)");
    }
    else
    {
        notifier = new NtfyNotifier(new NtfyCredentialsProvider());
    }

    var localIp = GetLocalIpAddress();

    _ = notifier.NotifyAsync($"""
        ▶ Reminder started
        Admin API: http://{localIp}:5000
        Seq: http://{localIp}:5341
        """, NtfyTopics.Reminders);

    var dailyDigestProcessor = new DailyDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.DailyDigest);
    var reminderProcessor = new ReminderProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.Reminders);
    var weeklyDigestProcessor = new WeeklyDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.WeeklyDigest);
    var twoWeekDigestProcessor = new TwoWeekDigestProcessor(dateTimeProvider, fileStorage, notifier, NtfyTopics.TwoWeekDigest);
    var printer = new EventOutputPrinter(dateTimeProvider);

    IEventReader eventReader;
    IGitHubClient? gitHubClient = null;

    // Configure GitHub options from appconfig.json with environment variable substitution
    var githubUrl = GetEnvVar("GITHUB_URL") ?? throw new InvalidOperationException("GITHUB_URL environment variable is not set.");
    var githubToken = GetEnvVar("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set.");

    builder.Services.Configure<GitHubOptions>(options =>
    {
        options.Url = githubUrl;
        options.Token = githubToken;
    });

    // Validate GitHub configuration
    var gitHubOptions = new GitHubOptions
    {
        Url = githubUrl,
        Token = githubToken
    };
    gitHubOptions.Validate();

    gitHubClient = new GitHubClient(gitHubOptions);

    if (DebugHelper.IsDebug)
    {
        //eventReader = new DebugEventReader();
        eventReader = new GitHubEventReader(gitHubClient, new YamlDotNetParser());
        log.Information("DEBUG MODE: используется GitHubEventReader (GitHubClient доступен для тестирования)");
    }
    else
    {
        eventReader = new GitHubEventReader(gitHubClient, new YamlDotNetParser());
        log.Information("RELEASE MODE: используется GitHubEventReader с YamlDotNetParser");
    }

    // Register EventReader for DI
    builder.Services.AddSingleton(eventReader);

    var runner = new EventRunner(
        dateTimeProvider,
        fileStorage,
        eventReader,
        new EventOutputPrinter(dateTimeProvider),
        dailyDigestProcessor,
        reminderProcessor,
        weeklyDigestProcessor,
        twoWeekDigestProcessor,
        printer);

    // Register EventRunner (MUST be before Build())
    builder.Services.AddSingleton(runner);

    // Build the app
    var app = builder.Build();

    // Serve static files from wwwroot
    app.UseDefaultFiles();
    app.UseStaticFiles();

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action}",
        defaults: new { controller = "Admin", action = "Index" });

    return (app, runner);
}

static string GetLocalIpAddress()
{
    try
    {
        var host = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
        return host.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "localhost";
    }
    catch
    {
        return "localhost";
    }
}

static ILogger ConfigureLogger()
{
    var config = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.WithProperty("Application", "Reminder")
        .Enrich.With<ShortClassNameEnricher>()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{ClassName,-20}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Seq("http://localhost:5341");

    // В DEBUG режиме фильтруем шумные ASP.NET Core логи
    if (DebugHelper.IsDebug)
    {
        config.Filter.ByExcluding(e =>
            e.Properties.ContainsKey("SourceContext") &&
            (e.Properties["SourceContext"].ToString().Contains("Microsoft.AspNetCore") ||
             e.Properties["SourceContext"].ToString().Contains("Microsoft.WebTools")));
    }

    Log.Logger = config.CreateLogger();
    return Log.ForContext(typeof(Program));
}

static string? GetEnvVar(string var) => Environment.GetEnvironmentVariable(var) ??
    Environment.GetEnvironmentVariable(var, EnvironmentVariableTarget.User);
