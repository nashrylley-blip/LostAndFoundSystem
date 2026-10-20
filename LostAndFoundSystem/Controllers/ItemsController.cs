using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;

namespace LostAndFoundSystem.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Items
        // Shows ONLY approved Lost & Found items
        public async Task<IActionResult> Index(
            string? searchString,
            int? categoryId)
        {
            var itemsQuery = _context.Items
                .Include(i => i.Category)
                .Where(i => i.ApprovalStatus == "Approved")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                itemsQuery = itemsQuery.Where(i =>
                    i.Title.Contains(searchString) ||
                    i.Description.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                itemsQuery = itemsQuery.Where(i =>
                    i.CategoryID == categoryId.Value);
            }

            ViewBag.Categories = await _context.Categories
                .ToListAsync();

            var items = await itemsQuery
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            return View(items);
        }

        // GET: /Items/Create
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories
                .ToListAsync();

            return View();
        }

        // POST: /Items/Create
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Item item,
            IFormFile? imageFile)
        {
            // Reports created here also need admin approval
            item.ApprovalStatus = "Pending";
            item.DateReported = DateTime.Now;

            if (ModelState.IsValid)
            {
                // Image Upload
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

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Your report has been submitted and is waiting for admin approval.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories
                .ToListAsync();

            return View(item);
        }
    }
}