using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReminderApp.Authentication;
using ReminderApp.Common;
using ReminderApp.EventProcessing;
using ReminderApp.EventReading;
using ReminderApp.EventWriting;
using ReminderApp.GitHubApi;

namespace ReminderApp.Controllers;

[Authorize(Policy = AdminAuthenticationExtensions.AdminPolicy)]
public class ApiController : Controller
{
    private readonly EventRunner _runner;
    private readonly IEventReader _eventReader;
    private readonly IEventWriter _eventWriter;

    public ApiController(EventRunner runner, IEventReader eventReader, IEventWriter eventWriter)
    {
        _runner = runner;
        _eventReader = eventReader;
        _eventWriter = eventWriter;
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
                    key = e.GetKey(),
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

    [HttpPost("events/update-date")]
    public async Task<IActionResult> UpdateEventDate([FromBody] UpdateEventDateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.NewDate))
            {
                return Json(new { success = false, message = "Key and NewDate are required" });
            }
            
            if (!DateOnly.TryParse(request.NewDate, out var newDate))
            {
                return Json(new { success = false, message = "Invalid date format" });
            }
            
            var result = await _eventWriter.UpdateEventDateAsync(request.Key, newDate);
            
            if (result.Success)
            {
                return Json(new { success = true, message = $"Date updated to {request.NewDate}" });
            }
            else
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Failed to update date" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("events/delete")]
    public async Task<IActionResult> DeleteEvent([FromBody] DeleteEventRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Key))
            {
                return Json(new { success = false, message = "Key is required" });
            }
            
            var result = await _eventWriter.DeleteEventAsync(request.Key);
            
            if (result.Success)
            {
                return Json(new { success = true, message = "Event deleted" });
            }
            else
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Failed to delete event" });
            }
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
