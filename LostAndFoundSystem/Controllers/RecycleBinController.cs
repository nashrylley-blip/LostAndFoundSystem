using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RecycleBinController : Controller
    {
        // Records stay in the Recycle Bin this many days before they are purged
        public const int RetentionDays = 30;

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RecycleBinController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /RecycleBin
        [HttpGet]
        public async Task<IActionResult> Index(string type = "All", string? q = null, string range = "Any")
        {
            if (type != "Items" && type != "Notifications")
                type = "All";

            if (range != "Today" && range != "7" && range != "30")
                range = "Any";

            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            // IgnoreQueryFilters() is what lets us see the soft-deleted records
            var itemsQuery = _context.Items
                .IgnoreQueryFilters()
                .Include(i => i.Category)
                .Include(i => i.ReportedByUser)
                .AsNoTracking()
                .Where(i => i.IsDeleted);

            var notifQuery = _context.Notifications
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(n => n.IsDeleted);

            // Search
            if (q != null)
            {
                itemsQuery = itemsQuery.Where(i =>
                    i.Title.Contains(q) ||
                    i.Description.Contains(q) ||
                    i.LocationFound.Contains(q));

                notifQuery = notifQuery.Where(n =>
                    n.Title.Contains(q) ||
                    n.Message.Contains(q));
            }

            // Date deleted
            DateTime? since = range switch
            {
                "Today" => DateTime.Today,
                "7" => DateTime.Now.AddDays(-7),
                "30" => DateTime.Now.AddDays(-30),
                _ => null
            };

            if (since.HasValue)
            {
                var s = since.Value;
                itemsQuery = itemsQuery.Where(i => i.DeletedAt >= s);
                notifQuery = notifQuery.Where(n => n.DeletedAt >= s);
            }

            var vm = new RecycleBinViewModel
            {
                Type = type,
                Search = q,
                Range = range,
                RetentionDays = RetentionDays,
                ItemsCount = await itemsQuery.CountAsync(),
                NotificationsCount = await notifQuery.CountAsync()
            };

            var items = new List<Item>();
            var notifications = new List<Notification>();

            if (type == "All" || type == "Items")
                items = await itemsQuery.ToListAsync();

            if (type == "All" || type == "Notifications")
                notifications = await notifQuery.ToListAsync();

            // Names of the admins who deleted the records
            var adminIds = items.Where(i => i.DeletedByUserID.HasValue).Select(i => i.DeletedByUserID!.Value)
                .Concat(notifications.Where(n => n.DeletedByUserID.HasValue).Select(n => n.DeletedByUserID!.Value))
                .Distinct()
                .ToList();

            var adminNames = await _context.Users
                .Where(u => adminIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u.FullName);

            string NameOf(int? userId) =>
                userId.HasValue && adminNames.TryGetValue(userId.Value, out var name) ? name : "—";

            foreach (var i in items)
            {
                var deletedAt = i.DeletedAt ?? DateTime.Now;
                vm.Entries.Add(new RecycleBinEntry
                {
                    Kind = "Item",
                    Id = i.ItemID,
                    Title = i.Title,
                    Details = string.Join(" · ", new[]
                    {
                        i.Category?.CategoryName,
                        string.IsNullOrWhiteSpace(i.LocationFound) ? null : i.LocationFound,
                        i.ReportedByUser != null ? "Reported by " + i.ReportedByUser.FullName : null
                    }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Badge = i.Status,
                    ImageUrl = i.ImageUrl,
                    DeletedAt = deletedAt,
                    DeletedBy = NameOf(i.DeletedByUserID),
                    DaysLeft = DaysLeftFor(deletedAt)
                });
            }

            foreach (var n in notifications)
            {
                var deletedAt = n.DeletedAt ?? DateTime.Now;
                vm.Entries.Add(new RecycleBinEntry
                {
                    Kind = "Notification",
                    Id = n.NotificationID,
                    Title = n.Title,
                    Details = n.Message,
                    Badge = n.Type,
                    DeletedAt = deletedAt,
                    DeletedBy = NameOf(n.DeletedByUserID),
                    DaysLeft = DaysLeftFor(deletedAt)
                });
            }

            vm.Entries = vm.Entries
                .OrderByDescending(e => e.DeletedAt)
                .ToList();

            return View(vm);
        }

        // POST: /RecycleBin/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string kind, int id, string type = "All", string? q = null, string range = "Any")
        {
            if (kind == "Item")
            {
                var item = await _context.Items
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(i => i.ItemID == id && i.IsDeleted);

                if (item == null)
                {
                    TempData["ErrorMessage"] = "That item is no longer in the Recycle Bin.";
                    return BackToIndex(type, q, range);
                }

                item.IsDeleted = false;
                item.DeletedAt = null;
                item.DeletedByUserID = null;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"\"{item.Title}\" was restored.";
            }
            else if (kind == "Notification")
            {
                var notif = await _context.Notifications
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(n => n.NotificationID == id && n.IsDeleted);

                if (notif == null)
                {
                    TempData["ErrorMessage"] = "That notification is no longer in the Recycle Bin.";
                    return BackToIndex(type, q, range);
                }

                notif.IsDeleted = false;
                notif.DeletedAt = null;
                notif.DeletedByUserID = null;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Notification was restored.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unknown record type.";
            }

            return BackToIndex(type, q, range);
        }

        // POST: /RecycleBin/DeletePermanently
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanently(string kind, int id, string type = "All", string? q = null, string range = "Any")
        {
            if (kind == "Item")
            {
                var item = await _context.Items
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(i => i.ItemID == id && i.IsDeleted);

                if (item == null)
                {
                    TempData["ErrorMessage"] = "That item is no longer in the Recycle Bin.";
                    return BackToIndex(type, q, range);
                }

                // Claims are hidden while the item is deleted, so ignore the filter to remove them too
                var claims = await _context.Claims
                    .IgnoreQueryFilters()
                    .Where(c => c.ItemID == id)
                    .ToListAsync();

                _context.Claims.RemoveRange(claims);

                var title = item.Title;
                var imageUrl = item.ImageUrl;

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                DeleteImageFile(imageUrl);

                TempData["SuccessMessage"] = $"\"{title}\" was permanently deleted.";
            }
            else if (kind == "Notification")
            {
                var notif = await _context.Notifications
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(n => n.NotificationID == id && n.IsDeleted);

                if (notif == null)
                {
                    TempData["ErrorMessage"] = "That notification is no longer in the Recycle Bin.";
                    return BackToIndex(type, q, range);
                }

                _context.Notifications.Remove(notif);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Notification was permanently deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unknown record type.";
            }

            return BackToIndex(type, q, range);
        }

        // ===== Helpers =====

        private IActionResult BackToIndex(string type, string? q, string range)
        {
            return RedirectToAction(nameof(Index), new { type, q, range });
        }

        private static int DaysLeftFor(DateTime deletedAt)
        {
            int daysPassed = (int)(DateTime.Now - deletedAt).TotalDays;
            return Math.Max(0, RetentionDays - daysPassed);
        }

        // Removes the uploaded picture of a permanently deleted item (only files inside wwwroot/uploads)
        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/"))
                return;

            try
            {
                var fileName = Path.GetFileName(imageUrl);
                if (string.IsNullOrEmpty(fileName))
                    return;

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(webRoot, "uploads", fileName);

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch
            {
                // The record is already deleted; a leftover image file is not worth failing the request.
            }
        }
    }
}