namespace ReminderApp;

public static class DebugHelper
{
    public static bool IsDebug => 
#if DEBUG
        true;
#else
        false;
#endif

    public static int LoopDelayMs => 
#if DEBUG
        5_000;
#else
        60_000;
#endif

    public static string AdminToken => 
#if DEBUG
        "1234";
#else
        Environment.GetEnvironmentVariable("ADMIN_API_TOKEN") ?? string.Empty;
#endif
}