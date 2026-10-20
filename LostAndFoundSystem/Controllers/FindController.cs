using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class FindController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public FindController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: /Find
        [HttpGet]
        public async Task<IActionResult> Index(string? searchString, int? categoryId, string? status)
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.ReportedByUser)
                .Where(i => i.ApprovalStatus == "Approved" && i.Status != "Claimed")
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(i =>
                    i.Title.Contains(searchString) ||
                    i.Description.Contains(searchString) ||
                    i.LocationFound.Contains(searchString));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && (status == "Lost" || status == "Found"))
            {
                query = query.Where(i => i.Status == status);
            }

            var items = await query
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.TotalItems = items.Count;

            return View(items);
        }

        // POST: /Find/Claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(int itemId, string proofOfOwnership)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(proofOfOwnership))
            {
                TempData["ErrorMessage"] = "Please provide proof of ownership or additional details.";
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.Items.FirstOrDefaultAsync(i =>
                i.ItemID == itemId &&
                i.ApprovalStatus == "Approved" &&
                i.Status != "Claimed");

            if (item == null)
            {
                TempData["ErrorMessage"] = "Item not found or is no longer available for claiming.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent claiming own item
            if (item.ReportedByUserID == userId)
            {
                TempData["ErrorMessage"] = "You cannot claim an item that you reported.";
                return RedirectToAction(nameof(Index));
            }

            // Check if already has a pending claim
            var existing = await _context.Claims
                .AnyAsync(c => c.ItemID == itemId &&
                               c.ClaimedByUserID == userId &&
                               c.Status == "Pending");

            if (existing)
            {
                TempData["ErrorMessage"] = "You already have a pending claim for this item.";
                return RedirectToAction(nameof(Index));
            }

            var claim = new ItemClaim
            {
                ItemID = itemId,
                ClaimedByUserID = userId,
                ProofOfOwnership = proofOfOwnership.Trim(),
                Status = "Pending",
                DateClaimed = DateTime.Now
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            await _notifications.NotifyAdminsAsync(
                "New Item Claim",
                $"A student submitted a claim for \"{item.Title}\"",
                "Claim",
                item.ItemID);

            TempData["SuccessMessage"] = "Your claim has been submitted and is waiting for admin review.";
            return RedirectToAction(nameof(Index));
        }
    }
}
