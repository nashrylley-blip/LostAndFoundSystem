
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public ManageReportsController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: /ManageReports
        [HttpGet]
        public async Task<IActionResult> Index(
            string filter = "Pending Reports")
        {
            var reportsQuery = _context.Items
                .Include(i => i.Category)
                .Include(i => i.ReportedByUser)
                .AsNoTracking()
                .AsQueryable();

            switch (filter)
            {
                case "Approved Reports":
                    reportsQuery = reportsQuery
                        .Where(i => i.ApprovalStatus == "Approved");
                    break;

                case "Rejected Reports":
                    reportsQuery = reportsQuery
                        .Where(i => i.ApprovalStatus == "Rejected");
                    break;

                case "Lost Items":
                    reportsQuery = reportsQuery
                        .Where(i =>
                            i.Status == "Lost" &&
                            i.ApprovalStatus == "Pending");
                    break;

                case "Found Items":
                    reportsQuery = reportsQuery
                        .Where(i =>
                            i.Status == "Found" &&
                            i.ApprovalStatus == "Pending");
                    break;

                case "All Reports":
                    break;

                default:
                    reportsQuery = reportsQuery
                        .Where(i => i.ApprovalStatus == "Pending");
                    filter = "Pending Reports";
                    break;
            }

            var reports = await reportsQuery
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewBag.TotalReports = reports.Count;

            return View(reports);
        }

        // GET: /ManageReports/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ReportedByUser)
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        // POST: /ManageReports/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var report = await _context.Items
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (report == null)
            {
                return NotFound();
            }

            if (report.ApprovalStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This report has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            report.ApprovalStatus = "Approved";

            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(
                report.ReportedByUserID,
                "Report Approved",
                $"Your report \"{report.Title}\" has been approved and is now posted.",
                "Report",
                report.ItemID);

            TempData["SuccessMessage"] =
                "Report approved successfully. It is now officially posted.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /ManageReports/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? rejectionReason)
        {
            var report = await _context.Items
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (report == null)
            {
                return NotFound();
            }

            if (report.ApprovalStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This report has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            // Reason is required
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] =
                    "Please provide a reason for rejecting the report.";

                return RedirectToAction(nameof(Details), new { id });
            }

            report.ApprovalStatus = "Rejected";
            report.RejectionReason = rejectionReason.Trim();

            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(
                report.ReportedByUserID,
                "Report Rejected",
                $"Your report \"{report.Title}\" has been rejected. Reason: {report.RejectionReason}",
                "Report",
                report.ItemID);

            TempData["SuccessMessage"] =
                "Report rejected successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /ManageReports/MarkClaimed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkClaimed(int id)
        {
            var report = await _context.Items
                .FirstOrDefaultAsync(i => i.ItemID == id);

            if (report == null)
            {
                return NotFound();
            }

            if (report.ApprovalStatus != "Approved")
            {
                TempData["ErrorMessage"] =
                    "Only approved reports can be marked as claimed.";

                return RedirectToAction(nameof(Index));
            }

            report.Status = "Claimed";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Item has been marked as claimed.";

            return RedirectToAction(nameof(Index));
        }
    }
}