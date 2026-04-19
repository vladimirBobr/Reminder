using System.Net;

namespace ReminderApp.EventNotification;

public static class ProxyHelper
{
    public static WebProxy? CreateProxy()
    {
        // В DEBUG возвращаем null (без прокси для удобства отладки)
        if (DebugHelper.IsDebug)
            return null;

        // TODO: Раскомментировать для использования прокси в production
        // if (OperatingSystem.IsWindows())
        // {
        //     var webProxy = new WebProxy("", 9090);
        //     webProxy.UseDefaultCredentials = true;
        //     return webProxy;
        // }

        return null;
    }
}
