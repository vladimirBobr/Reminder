using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReminderApp.Authentication;
using ReminderApp.Common;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading;
using ReminderApp.GitHubApi;

namespace ReminderApp.Controllers;

[Authorize(Policy = AdminAuthenticationExtensions.AdminPolicy)]
public class ApiController : Controller
{
    private readonly EventRunner _runner;
    private readonly IEventReader _eventReader;

    public ApiController(EventRunner runner, IEventReader eventReader)
    {
        _runner = runner;
        _eventReader = eventReader;
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

    // ==================== Events API ====================

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        try
        {
            var data = await _eventReader.ReadEventsAsync();
            return Json(new
            {
                success = true,
                events = data.Events.Select(e => new
                {
                    date = e.Date.ToString("yyyy-MM-dd"),
                    time = e.Time?.ToString("HH:mm"),
                    subject = e.Subject,
                    description = e.Description
                }),
                shoppingItems = data.ShoppingItems.Select(s => s.Subject)
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("events")]
    public IActionResult UpdateEvents([FromBody] UpdateEventsRequest request)
    {
        try
        {
            if (request?.Events == null || request.Events.Count == 0)
            {
                return Json(new { success = false, message = "No events provided" });
            }

            // TODO: Implement GitHub file update logic
            // For now, just acknowledge the request
            return Json(new { success = true, message = "Events updated (GitHub sync not yet implemented)" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("events/delete")]
    public IActionResult DeleteEvent([FromBody] EventUpdateItem eventItem)
    {
        try
        {
            // TODO: Implement GitHub file update logic for deletion
            return Json(new { success = true, message = "Event deleted (GitHub sync not yet implemented)" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
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
