using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ReminderApp.EventProcessing;

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

    private static void StartAdminApi(EventRunner runner, string adminToken)
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
