using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading.GitHub;
using ReminderApp.EventStorage;

namespace ReminderApp;

public static class AdminApi
{
    public static void Start(EventRunner runner)
    {
        var adminToken = DebugHelper.AdminToken;

        if (string.IsNullOrEmpty(adminToken))
        {
            Log.Information("Добавь токен удалённого доступа:");

            // Сохраняем в переменные окружения для текущей сессии
            adminToken = Console.ReadLine();
            Environment.SetEnvironmentVariable("ADMIN_API_TOKEN", adminToken, EnvironmentVariableTarget.User);
            Log.Warning("Remote control is not configured");
            return;
        }

        if (DebugHelper.IsDebug)
            Log.Information("DEBUG: Using hardcoded admin token: {Token}", adminToken);

        _ = Task.Run(() => StartAdminApi(runner, adminToken));
    }

    private static async ValueTask<object?> AuthFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ctx = context.HttpContext;
        var adminToken = ctx.Items["AdminToken"]?.ToString() ?? "";
        
        var token = ctx.Request.Query["token"].FirstOrDefault();
        var isLocalhost = ctx.Connection.RemoteIpAddress?.ToString() == "127.0.0.1"
                       || ctx.Connection.RemoteIpAddress?.ToString() == "::1";

        if (isLocalhost && token == "test")
            return await next(context);

        if (string.IsNullOrEmpty(token) || token != adminToken)
        {
            Log.Warning("Unauthorized attempt from {Ip}", ctx.Connection.RemoteIpAddress);
            return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
        }

        return await next(context);
    }

    private static void StartAdminApi(EventRunner runner, string adminToken)
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.Use(async (ctx, next) =>
        {
            ctx.Items["AdminToken"] = adminToken;
            await next();
        });

        var protectedGroup = app.MapGroup("").AddEndpointFilter(AuthFilter);

        protectedGroup.MapGet("/today", (HttpContext ctx) =>
        {
            _ = Task.Run(() => runner.SendDigest());
            return Results.Json(new { message = "Digest sent" });
        });

        protectedGroup.MapGet("/week", (HttpContext ctx) =>
        {
            _ = Task.Run(() => runner.SendWeeklyDigest());
            return Results.Json(new { message = "Weekly digest sent" });
        });

        protectedGroup.MapGet("/add-note", (HttpContext ctx) =>
        {
            var note = ctx.Request.Query["note"].FirstOrDefault();
            if (string.IsNullOrEmpty(note))
            {
                return Results.Json(new { error = "Note is required" }, statusCode: 400);
            }

            var notesService = new NotesService(new GitHubCredentialsProvider());
            var error = notesService.AddNote(note);

            if (!string.IsNullOrEmpty(error))
            {
                return Results.Json(new { message = error });
            }

            return Results.Json(new { message = "Ok" });
        });

        app.MapPost("/github-webhook", async (HttpContext ctx) =>
        {
            try
            {
                var payload = await ParsePayload(ctx);
                if (payload == null)
                    return Results.BadRequest("Invalid payload");

                Log.Information("Web hook received");

                return Results.Accepted("post-processing started");
            }
            catch (Exception ex)
            {
                return Results.InternalServerError(ex.Message);
            }
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
