using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminNotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminNotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            // Messages are handled by the envelope, so they are not shown here.
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId.Value && n.Type != "Message")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Load the related items (with category + reporter) so the
            // expanded "View" panel can show the report details and image.
            var itemIds = notifications
                .Where(n => n.RelatedItemID.HasValue)
                .Select(n => n.RelatedItemID!.Value)
                .Distinct()
                .ToList();

            ViewBag.Items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ReportedByUser)
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.ItemID))
                .ToDictionaryAsync(i => i.ItemID);

            ViewBag.UnreadCount = notifications.Count(n => !n.IsRead);
            return View(notifications);
        }

        // Called with fetch() when the admin clicks "View" on an unread notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReadAjax(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId.Value);

            if (notif == null)
                return NotFound();

            if (!notif.IsRead)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId.Value);

            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var unread = await _context.Notifications
                .Where(n => n.UserID == userId.Value && !n.IsRead && n.Type != "Message")
                .ToListAsync();

            foreach (var n in unread)
                n.IsRead = true;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId.Value);

            if (notif != null)
            {
                // Soft delete: move to the Recycle Bin instead of removing from the database
                notif.IsDeleted = true;
                notif.DeletedAt = DateTime.Now;
                notif.DeletedByUserID = userId.Value;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Notification moved to the Recycle Bin.";
            }

            return RedirectToAction(nameof(Index));
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}