using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ReminderApp.DateTimeProviding;
using ReminderApp.EventNotification;
using ReminderApp.EventNotification.Ntfy;
using ReminderApp.EventNotification.YandexMail;
using ReminderApp.EventOutput;
using ReminderApp.EventProcessing;
using ReminderApp.EventProcessing.Senders;
using ReminderApp.EventReading.GitHub;
using ReminderApp.FileStorage;

namespace ReminderApp;

internal class Program
{
    static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        Log.Information("▶️ Starting Reminder");

        NtfyNotifier ntfyNotifier = new(new NtfyCredentialsProvider());
        await ntfyNotifier.NotifyAsync("▶️ Reminder started");

        var dateTimeProvider = new DateTimeProvider();
        var fileStorage = new JsonFileStorage();

        // Создаём список нотификаторов
        var notifiers = new List<INotifier>
        {
            ntfyNotifier,
            new YandexMailNotifier(new YandexMailCredentialsProvider()),
            // new TelegramNotifier(new TelegramCredentialsProvider()),
        };

        // Создаём отправителей
        var digestSender = new DigestSender(dateTimeProvider, fileStorage, notifiers);
        var reminderSender = new ReminderSender(dateTimeProvider, fileStorage, notifiers);
        var printer = new EventOutputPrinter();

        var runner = new EventRunner(
            dateTimeProvider,
            fileStorage,
            new GitHubEventReader(new GitHubCredentialsProvider()),
            new EventOutputPrinter(),
            digestSender,
            reminderSender,
            printer);

        SetupAdminApi(runner);

        await runner.StartAsync();

        await Task.Delay(-1);
    }

    private static void SetupAdminApi(EventRunner runner)
    {
        var adminToken = Environment.GetEnvironmentVariable("ADMIN_API_TOKEN");

        if (string.IsNullOrEmpty(adminToken))
        {
#if DEBUG
            Log.Information("Добавь токен удалённого доступа:");

            // Сохраняем в переменные окружения для текущей сессии
            adminToken = Console.ReadLine();
            Environment.SetEnvironmentVariable("ADMIN_API_TOKEN", adminToken, EnvironmentVariableTarget.User);
#else

            Log.Warning("Remote control is not configured");
            return;
#endif
        }

        _ = Task.Run(() => StartAdminApi(runner, adminToken));
    }

    static void StartAdminApi(EventRunner runner, string adminToken)
    {
        
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/today", async (HttpContext ctx) =>
        {
            var token = ctx.Request.Query["token"].FirstOrDefault();

            if (string.IsNullOrEmpty(token) || token != adminToken)
            {
                Log.Warning("Unauthorized stop attempt from {Ip}", ctx.Connection.RemoteIpAddress);
                return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
            }


            _ = Task.Run(() => runner.SendDigest());

            return Results.Json(new { message = "Digest sent" });
        });

        app.MapPost("/github-webhook", async (HttpContext ctx) =>
        {
            var payload = await ParsePayload(ctx);
            if (payload == null)
                return Results.BadRequest("Invalid payload");

            // Проверяем, что коммит не от самого бота (защита от цикла)
            //var author = payload.HeadCommit?.Author?.Username;
            //if (author == "reminder-bot" || author == "vladimirBobr")
            //    return Results.Ok("ignored: bot commit");

            Log.Information("Web hook received");

            return Results.Accepted("post-processing started");
        });

        app.Run("http://0.0.0.0:5000");
    }

    private static async Task<GitHubPushEvent?> ParsePayload(HttpContext ctx)
    {
        var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
            return null;

        return JsonSerializer.Deserialize<GitHubPushEvent>(body);
    }

    public class GitHubPushEvent
    {
        [JsonPropertyName("head_commit")]
        public GitHubCommit HeadCommit { get; set; } = new();
    }

    public class GitHubCommit
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public GitHubAuthor? Author { get; set; }
    }

    public class GitHubAuthor
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }
}
