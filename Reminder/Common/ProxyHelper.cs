using System.Net;

namespace ReminderApp.EventNotification;

public static class ProxyHelper
{
    public static WebProxy? CreateProxy()
    {
#if DEBUG
        return null;

        //if (OperatingSystem.IsWindows())
        //{
        //    var webProxy = new WebProxy("", 9090);
        //    webProxy.UseDefaultCredentials = true;
        //    return webProxy;
        //}

#else
        
#endif
    }
}
