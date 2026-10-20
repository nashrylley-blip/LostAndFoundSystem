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
    public class ReportLostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public ReportLostController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: /ReportLost
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: /ReportLost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item, IFormFile? imageFile)
        {
            // Set report information
            item.Status = "Lost";
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
                // Handle image upload
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
                    "New Lost Item Report",
                    $"A new lost item report was submitted: \"{item.Title}\"",
                    "Report",
                    item.ItemID);

                TempData["SuccessMessage"] =
                    "Your lost item report has been submitted and is waiting for admin approval.";

                return RedirectToAction("Index", "ReportLost");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View("Index", item);
        }
    }
}