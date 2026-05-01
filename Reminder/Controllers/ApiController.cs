using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReminderApp.Authentication;
using ReminderApp.EventProcessing;
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

    [HttpPost]
    public IActionResult Today()
    {
        _ = Task.Run(() => _runner.SendDigest());
        return Json(new { success = true, message = "Рассылка отправлена" });
    }

    [HttpPost]
    public IActionResult Week()
    {
        _ = Task.Run(() => _runner.SendWeeklyDigest());
        return Json(new { success = true, message = "Недельная рассылка отправлена" });
    }

    [HttpPost]
    public IActionResult TwoWeek()
    {
        _ = Task.Run(() => _runner.SendTwoWeekDigest());
        return Json(new { success = true, message = "Рассылка на две недели отправлена" });
    }

    // ==================== Notes API ====================

    //[HttpPost]
    //public IActionResult AddNote([FromForm] string note, [FromForm] string date)
    //{
    //    if (string.IsNullOrEmpty(note))
    //    {
    //        return Json(new { success = false, message = "Note is required" });
    //    }

    //    DateOnly? parsedDate = null;
    //    if (!string.IsNullOrEmpty(date))
    //    {
    //        if (DateOnly.TryParseExact(date, "dd.MM.yyyy",
    //            System.Globalization.CultureInfo.InvariantCulture,
    //            System.Globalization.DateTimeStyles.None, out var d))
    //        {
    //            parsedDate = d;
    //        }
    //        else
    //        {
    //            return Json(new { success = false, message = "Invalid date format. Use dd.MM.yyyy" });
    //        }
    //    }

    //    var gitHubClient = new GitHubClient(new GitHubCredentialsProvider());
    //    var notesService = new NotesService(gitHubClient);
    //    var (error, message) = notesService.AddNote(note, parsedDate);

    //    if (!string.IsNullOrEmpty(error))
    //    {
    //        return Json(new { success = false, message = error });
    //    }
    //    else
    //    {
    //        return Json(new { success = true, message = message ?? "Note added" });
    //    }
    //}

    // ==================== Shopping API ====================

    //[HttpPost]
    //public IActionResult AddShoppingItem([FromForm] string item)
    //{
    //    if (string.IsNullOrEmpty(item))
    //    {
    //        return Json(new { success = false, message = "Item is required" });
    //    }

    //    var gitHubClient = new GitHubClient(new GitHubCredentialsProvider());
    //    var shopListService = new ShopListService(gitHubClient);
    //    var (error, message) = shopListService.AddItem(item);

    //    if (!string.IsNullOrEmpty(error))
    //    {
    //        return Json(new { success = false, message = error });
    //    }
    //    else
    //    {
    //        return Json(new { success = true, message = message ?? "Item added" });
    //    }
    //}

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
