using Microsoft.AspNetCore.Mvc;
using ReminderApp.EventProcessing;
using Microsoft.AspNetCore.Http;

namespace ReminderApp.Controllers;

public class AdminController : Controller
{
    private readonly EventRunner _runner;

    public AdminController(EventRunner runner)
    {
        _runner = runner;
    }

    public IActionResult Index()
    {
        var isLoggedIn = IsAuthorized();
        var model = new AdminIndexViewModel
        {
            IsLoggedIn = isLoggedIn,
            AdminToken = DebugHelper.AdminToken
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult LoginPost(string token)
    {
        if (!string.IsNullOrEmpty(token) && token == DebugHelper.AdminToken)
        {
            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict
            });
        }
        return Redirect("/admin");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("token");
        return Redirect("/admin");
    }

    private bool IsAuthorized()
    {
        var cookieToken = HttpContext.Request.Cookies["token"];
        return !string.IsNullOrEmpty(cookieToken) && cookieToken == DebugHelper.AdminToken;
    }
}

public class AdminIndexViewModel
{
    public bool IsLoggedIn { get; set; }
    public string? AdminToken { get; set; }
}