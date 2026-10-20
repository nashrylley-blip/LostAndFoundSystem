using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class ReportFoundController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public ReportFoundController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: /ReportFound
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryID)
                .ToListAsync();

            return View();
        }

        // POST: /ReportFound
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Item item, IFormFile? imageFile)
        {
            // Set report information
            item.Status = "Found";
            item.ApprovalStatus = "Pending";
            item.DateReported = DateTime.Now;

            // Get logged-in user's ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                item.ReportedByUserID = userId;
            }

            ModelState.Remove("Category");
            ModelState.Remove("ReportedByUser");

            if (ModelState.IsValid)
            {
                // Upload image if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads"
                    );

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName =
                        Guid.NewGuid().ToString() + "_" + imageFile.FileName;

                    string filePath = Path.Combine(
                        uploadsFolder,
                        uniqueFileName
                    );

                    using (var fileStream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    item.ImageUrl = "/uploads/" + uniqueFileName;
                }

                // Save report as Pending
                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                await _notifications.NotifyAdminsAsync(
                    "New Found Item Report",
                    $"A new found item report was submitted: \"{item.Title}\"",
                    "Report",
                    item.ItemID);

                TempData["SuccessMessage"] =
                    "Found item report has been submitted and is waiting for admin approval.";

                return RedirectToAction("Index", "ReportFound");
            }

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryID)
                .ToListAsync();

            return View("Index", item);
        }
    }
}