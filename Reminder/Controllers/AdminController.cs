using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReminderApp.Authentication;
using ReminderApp.EventProcessing;

namespace ReminderApp.Controllers;

public class AdminController : Controller
{
    private readonly EventRunner _runner;

    public AdminController(EventRunner runner)
    {
        _runner = runner;
    }

    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View();

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> LoginPost(string token)
    {
        if (!string.IsNullOrEmpty(token) && token == DebugHelper.AdminToken)
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Admin"),
                new System.Security.Claims.Claim("Token", token)
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, AdminAuthenticationExtensions.AdminCookieScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                AdminAuthenticationExtensions.AdminCookieScheme,
                principal,
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(90)
                });
        }
        return Redirect("/");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AdminAuthenticationExtensions.AdminCookieScheme);
        return RedirectToAction("Login");
    }
}
