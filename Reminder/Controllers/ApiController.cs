using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReminderApp.Authentication;
using ReminderApp.EventProcessing;
using ReminderApp.EventStorage;
using ReminderApp.GitHubApi;

namespace ReminderApp.Controllers;

[Authorize(Policy = AdminAuthenticationExtensions.AdminPolicy)]
public class ApiController : Controller
{
    private readonly EventRunner _runner;

    public ApiController(EventRunner runner)
    {
        _runner = runner;
    }

    // ==================== Digest API ====================

    [HttpGet]
    public IActionResult Today()
    {
        _ = Task.Run(() => _runner.SendDigest());
        TempData["Message"] = "Рассылка отправлена";
        return RedirectToAction("Index", "Admin");
    }

    [HttpGet]
    public IActionResult Week()
    {
        _ = Task.Run(() => _runner.SendWeeklyDigest());
        TempData["Message"] = "Недельная рассылка отправлена";
        return RedirectToAction("Index", "Admin");
    }

    [HttpGet]
    public IActionResult TwoWeek()
    {
        _ = Task.Run(() => _runner.SendTwoWeekDigest());
        TempData["Message"] = "Рассылка на две недели отправлена";
        return RedirectToAction("Index", "Admin");
    }

    // ==================== Notes API ====================

    [HttpGet]
    public IActionResult AddNote(string? note, string? date)
    {
        if (string.IsNullOrEmpty(note))
        {
            TempData["Error"] = "Note is required";
            return RedirectToAction("Index", "Admin");
        }

        DateOnly? parsedDate = null;
        if (!string.IsNullOrEmpty(date))
        {
            if (DateOnly.TryParseExact(date, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
            {
                parsedDate = d;
            }
            else
            {
                TempData["Error"] = "Invalid date format. Use dd.MM.yyyy";
                return RedirectToAction("Index", "Admin");
            }
        }

        var gitHubClient = new GitHubClient(new GitHubCredentialsProvider());
        var notesService = new NotesService(gitHubClient);
        var (error, message) = notesService.AddNote(note, parsedDate);

        if (!string.IsNullOrEmpty(error))
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Message"] = message ?? "Note added";
        }

        return RedirectToAction("Index", "Admin");
    }

    // ==================== Shopping API ====================

    [HttpGet]
    public IActionResult AddShoppingItem(string? item)
    {
        if (string.IsNullOrEmpty(item))
        {
            TempData["Error"] = "Item is required";
            return RedirectToAction("Index", "Admin");
        }

        var gitHubClient = new GitHubClient(new GitHubCredentialsProvider());
        var shopListService = new ShopListService(gitHubClient);
        var (error, message) = shopListService.AddItem(item);

        if (!string.IsNullOrEmpty(error))
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Message"] = message ?? "Item added";
        }

        return RedirectToAction("Index", "Admin");
    }

    // ==================== Webhook ====================

    [AllowAnonymous]
    [HttpPost]
    public IActionResult GitHubWebhook()
    {
        // Webhook не требует авторизации (публичный endpoint для GitHub)
        TempData["Message"] = "Webhook received";
        return RedirectToAction("Index", "Admin");
    }
}