using System.Security.Claims;
using LostAndFoundSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.UnreadCount = notifications.Count(n => !n.IsRead);
            return View(notifications);
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
                .Where(n => n.UserID == userId.Value && !n.IsRead)
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
                _context.Notifications.Remove(notif);
                await _context.SaveChangesAsync();
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
