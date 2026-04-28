using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading.GitHub;
using ReminderApp.EventStorage;
using ReminderApp.GitHubApi;

namespace ReminderApp;

public static class AdminApi
{
    public static void Start(EventRunner runner, IGitHubClient? gitHubClient)
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

        _ = Task.Run(() => StartAdminApi(runner, adminToken, gitHubClient));
    }

    private static async ValueTask<object?> AuthFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ctx = context.HttpContext;
        var adminToken = ctx.Items["AdminToken"]?.ToString() ?? "";
        
        // Check cookie first
        var cookieToken = ctx.Request.Cookies["token"];
        if (!string.IsNullOrEmpty(cookieToken) && cookieToken == adminToken)
            return await next(context);

        // Check Authorization header (Bearer token)
        var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
        var bearerToken = authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? authHeader.Substring(7)
            : null;
        if (!string.IsNullOrEmpty(bearerToken) && bearerToken == adminToken)
            return await next(context);

        Log.Warning("Unauthorized attempt from {Ip}", ctx.Connection.RemoteIpAddress);
        return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
    }

    private static void StartAdminApi(EventRunner runner, string adminToken, IGitHubClient? gitHubClient)
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

        protectedGroup.MapGet("/two-week", (HttpContext ctx) =>
        {
            _ = Task.Run(() => runner.SendTwoWeekDigest());
            return Results.Json(new { message = "Two week digest sent" });
        });

        protectedGroup.MapGet("/add-note", (HttpContext ctx) =>
        {
            var note = ctx.Request.Query["note"].FirstOrDefault();
            if (string.IsNullOrEmpty(note))
            {
                return Results.Json(new { error = "Note is required" }, statusCode: 400);
            }

            DateOnly? date = null;
            var dateStr = ctx.Request.Query["date"].FirstOrDefault();
            Log.Information("[AdminApi] /add-note called - note: {Note}, raw dateStr: {DateStr}", note, dateStr);
            
            if (!string.IsNullOrEmpty(dateStr))
            {
                if (DateOnly.TryParseExact(dateStr, "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    date = parsedDate;
                    Log.Information("[AdminApi] Parsed date: {Date}", parsedDate);
                }
                else
                {
                    return Results.Json(new { error = "Invalid date format. Use dd.MM.yyyy" }, statusCode: 400);
                }
            }

            if (gitHubClient == null)
            {
                return Results.Json(new { error = "GitHub client not available in debug mode" }, statusCode: 400);
            }

            var notesService = new NotesService(gitHubClient);
            var (error, message) = notesService.AddNote(note, date);

            if (!string.IsNullOrEmpty(error))
            {
                return Results.Json(new { message = error });
            }

            return Results.Json(new { message = message ?? "Ok" });
        });

        protectedGroup.MapGet("/add-shopping-item", (HttpContext ctx) =>
        {
            var item = ctx.Request.Query["item"].FirstOrDefault();
            if (string.IsNullOrEmpty(item))
            {
                return Results.Json(new { error = "Item is required" }, statusCode: 400);
            }

            if (gitHubClient == null)
            {
                return Results.Json(new { error = "GitHub client not available in debug mode" }, statusCode: 400);
            }

            var shopListService = new ShopListService(gitHubClient);
            var (error, message) = shopListService.AddItem(item);

            if (!string.IsNullOrEmpty(error))
            {
                return Results.Json(new { message = error });
            }

            return Results.Json(new { message = message ?? "Ok" });
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

        app.MapGet("/login", () => Results.Text(@"<!DOCTYPE html>
<html>
<head><title>Login</title></head>
<body>
<h1>Login</h1>
<form method=""post"" action=""/login"">
<input type=""password"" name=""token"" placeholder=""Enter token"" required>
<button type=""submit"">Login</button>
</form>
</body>
</html>", "text/html"));

        app.MapPost("/login", (HttpContext ctx) =>
        {
            var token = ctx.Request.Form["token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token))
            {
                ctx.Response.Cookies.Append("token", token, new CookieOptions 
                { 
                    HttpOnly = true, 
                    SameSite = SameSiteMode.Strict 
                });
            }
            return Results.Redirect("/");
        });

        app.MapGet("/logout", (HttpContext ctx) =>
        {
            ctx.Response.Cookies.Delete("token");
            return Results.Redirect("/");
        });

        app.MapGet("/", (HttpContext ctx) =>
        {
            var isLoggedIn = !string.IsNullOrEmpty(ctx.Request.Cookies["token"]);
            
            var html = $@"<!DOCTYPE html>
<html>
<head><title>Admin API</title>
<link rel=""icon"" type=""image/svg+xml"" href=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' version='1.1' viewBox='0 0 200 200'%3E%3Crect width='200' height='200' fill='url(%23gradient)'/%3E%3Cdefs%3E%3ClinearGradient id='gradient' gradientTransform='rotate(0 0.5 0.5)'%3E%3Cstop offset='0%25' stop-color='%23c48f4d'/%3E%3Cstop offset='100%25' stop-color='%23985351'/%3E%3C/linearGradient%3E%3C/defs%3E%3Cg fill='%23084966' transform='matrix(12.659340659340659 0 0 12.659340659340659 16.32043168734718 190.007288644602)' stroke='%23e26122' stroke-width='0.3'%3E%3Cpath d='M3.77-14.22L6.59-3.87L9.42-14.22L13.25-14.22L8.47 0L4.72 0L-0.03-14.22L3.77-14.22Z'/%3E%3C/g%3E%3C/svg%3E"">
</head>
<body>
<h1>Admin API {(!isLoggedIn ? "(Not logged in)" : "")}</h1>
" + (isLoggedIn 
? @"<p><a href=""/logout"">Logout</a></p>
<ul>
<li><a href=""/today"">GET /today - Send today's digest</a></li>
<li><a href=""/week"">GET /week - Send weekly digest</a></li>
<li><a href=""/two-week"">GET /two-week - Send two week digest (14 days ahead)</a></li>
<li><a href=""/add-note?note=Test note"">GET /add-note?note={note} - Add note to events file (optional: &date=dd.MM.yyyy)</a></li>
<li>POST /github-webhook - GitHub webhook receiver</li>
</ul>" 
: @"<p>Please <a href=""/login"">login</a> to access protected endpoints.</p>
<p>For API calls use header: <code>Authorization: Bearer <token></code></p>") + @"
</body>
</html>";
            return Results.Text(html, "text/html");
        });

        app.Run("http://127.0.0.1:5000");
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
