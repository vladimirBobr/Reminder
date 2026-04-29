using Microsoft.AspNetCore.Builder;

namespace ReminderApp;

public static class AdminApi
{
    public static void Configure(WebApplication app)
    {
        var adminToken = DebugHelper.AdminToken;

        if (string.IsNullOrEmpty(adminToken))
        {
            if (DebugHelper.IsDebug)
            {
                Log.Information("Добавь токен удалённого доступа:");

                // Сохраняем в переменные окружения для текущей сессии
                adminToken = Console.ReadLine();
                Environment.SetEnvironmentVariable("ADMIN_API_TOKEN", adminToken, EnvironmentVariableTarget.User);
            }

            throw new Exception("Remote control is not configured");
        }

        if (DebugHelper.IsDebug)
            Log.Information("DEBUG: Using hardcoded admin token: {Token}", adminToken);

        // Middleware для передачи токена в HttpContext.Items
        app.Use(async (ctx, next) =>
        {
            ctx.Items["AdminToken"] = adminToken;
            await next();
        });
    }
}
