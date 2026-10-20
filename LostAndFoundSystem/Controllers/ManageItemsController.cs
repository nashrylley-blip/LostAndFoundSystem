using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManageItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ManageItems
        [HttpGet]
        public async Task<IActionResult> Index(string filter = "All Items")
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.ReportedByUser)
                .Where(i => i.ApprovalStatus == "Approved")
                .AsNoTracking()
                .AsQueryable();

            switch (filter)
            {
                case "Lost Items":
                    query = query.Where(i => i.Status == "Lost");
                    break;
                case "Found Items":
                    query = query.Where(i => i.Status == "Found");
                    break;
                case "Claimed Items":
                    query = query.Where(i => i.Status == "Claimed");
                    break;
                default:
                    filter = "All Items";
                    break;
            }

            var items = await query
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewBag.TotalItems = items.Count;

            return View(items);
        }

        // POST: /ManageItems/MarkClaimed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkClaimed(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == id);

            if (item == null)
                return NotFound();

            if (item.ApprovalStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Only approved items can be marked as claimed.";
                return RedirectToAction(nameof(Index));
            }

            item.Status = "Claimed";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Item has been marked as claimed.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /ManageItems/Delete/5
        // Soft delete: the item is moved to the Recycle Bin instead of being removed from the database.
        // Its claims and image are kept, so the item can be restored later.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemID == id);

            if (item == null)
                return NotFound();

            int? adminId = null;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int parsedId))
                adminId = parsedId;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.Now;
            item.DeletedByUserID = adminId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{item.Title}\" was moved to the Recycle Bin.";
            return RedirectToAction(nameof(Index));
        }
    }
}