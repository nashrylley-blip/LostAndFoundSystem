using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public ClaimsController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: /Claims
        public async Task<IActionResult> Index()
        {
            var claims = await _context.Claims
                .Include(c => c.Item)
                .Include(c => c.ClaimedByUser)
                .OrderByDescending(c => c.DateClaimed)
                .ToListAsync();

            return View(claims);
        }

        // POST: /Claims/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int claimId, string status)
        {
            if (status != "Approved" && status != "Rejected")
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(Index));
            }

            var claim = await _context.Claims
                .Include(c => c.Item)
                .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null)
            {
                return NotFound();
            }

            if (claim.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This claim has already been reviewed.";
                return RedirectToAction(nameof(Index));
            }

            claim.Status = status;

            if (status == "Approved" && claim.Item != null)
            {
                claim.Item.Status = "Claimed";
            }

            await _context.SaveChangesAsync();

            var itemTitle = claim.Item?.Title ?? "an item";
            if (status == "Approved")
            {
                await _notifications.NotifyAsync(
                    claim.ClaimedByUserID,
                    "Claim Approved",
                    $"Your claim for \"{itemTitle}\" has been approved.",
                    "Claim",
                    claim.ItemID);
            }
            else
            {
                await _notifications.NotifyAsync(
                    claim.ClaimedByUserID,
                    "Claim Rejected",
                    $"Your claim for \"{itemTitle}\" has been rejected.",
                    "Claim",
                    claim.ItemID);
            }

            TempData["SuccessMessage"] = status == "Approved"
                ? "Claim approved. Item marked as claimed."
                : "Claim rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}
