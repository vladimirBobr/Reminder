using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace ReminderApp.Authentication;

public static class AdminAuthenticationExtensions
{
    public const string AdminScheme = "AdminAuth";
    public const string AdminCookieScheme = "AdminCookie";
    public const string AdminPolicy = "AdminPolicy";

    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            // Cookie scheme handles browser authentication by default
            options.DefaultScheme = AdminCookieScheme;
            // Cookie scheme also handles challenge (redirects to login)
            options.DefaultChallengeScheme = AdminCookieScheme;
        })
        .AddCookie(AdminCookieScheme, options =>
        {
            options.Cookie.Name = "admin_token";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromDays(90);
            options.SlidingExpiration = true;
            options.LoginPath = "/Admin/Login";
            options.AccessDeniedPath = "/Admin/Login";
        })
        .AddScheme<AuthenticationSchemeOptions, AdminAuthenticationHandler>(AdminScheme, null);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy =>
                policy.RequireAuthenticatedUser()
                      .AddAuthenticationSchemes(AdminCookieScheme, AdminScheme));
        });

        return services;
    }
}

public class AdminAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AdminAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only check Authorization header for API clients
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            var token = authHeader.Replace("Token ", "").Replace("Bearer ", "");
            if (token == DebugHelper.AdminToken)
            {
                return Task.FromResult(CreateSuccessResult(token));
            }
        }

        // No Authorization header with valid token - don't fall through to cookie
        // because if we're here, there's no cookie (otherwise cookie handler would succeed)
        return Task.FromResult(AuthenticateResult.Fail("No valid Authorization header"));
    }

    private AuthenticateResult CreateSuccessResult(string token)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim("Token", token)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // This is called when authorization fails - for API clients return 401
        // For browser requests, the cookie scheme's challenge will redirect to login
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}