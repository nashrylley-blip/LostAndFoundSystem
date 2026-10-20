using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageUsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManageUsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ManageUsers
        [HttpGet]
        public async Task<IActionResult> Index(string filter = "All Users", string status = "All", string? q = null)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            // Role filter
            switch (filter)
            {
                case "Students":
                    query = query.Where(u => u.Role == "Student");
                    break;
                case "Administrators":
                    query = query.Where(u => u.Role == "Admin");
                    break;
                default:
                    filter = "All Users";
                    break;
            }

            // Status filter
            switch (status)
            {
                case "Active":
                    query = query.Where(u => !u.IsDeactivated);
                    break;
                case "Deactivated":
                    query = query.Where(u => u.IsDeactivated);
                    break;
                default:
                    status = "All";
                    break;
            }

            // Search (Student ID, name, email)
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            if (q != null)
            {
                query = query.Where(u =>
                    u.StudentID.Contains(q) ||
                    u.FullName.Contains(q) ||
                    u.Email.Contains(q));
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewBag.Status = status;
            ViewBag.Search = q;
            ViewBag.TotalUsers = users.Count;

            return View(users);
        }

        // POST: /ManageUsers/Deactivate/5
        // The account is kept (reports, claims, messages), but the user can no longer log in.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string filter = "All Users", string status = "All", string? q = null)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account.";
                return BackToIndex(filter, status, q);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound();

            if (user.Role == "Admin")
            {
                TempData["ErrorMessage"] = "Administrator accounts cannot be deactivated.";
                return BackToIndex(filter, status, q);
            }

            if (user.IsDeactivated)
            {
                TempData["ErrorMessage"] = "This account is already deactivated.";
                return BackToIndex(filter, status, q);
            }

            user.IsDeactivated = true;
            user.DeactivatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{LabelFor(user)}\" has been deactivated. They can no longer log in.";
            return BackToIndex(filter, status, q);
        }

        // POST: /ManageUsers/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id, string filter = "All Users", string status = "All", string? q = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound();

            if (!user.IsDeactivated)
            {
                TempData["ErrorMessage"] = "This account is already active.";
                return BackToIndex(filter, status, q);
            }

            user.IsDeactivated = false;
            user.DeactivatedAt = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{LabelFor(user)}\" has been reactivated.";
            return BackToIndex(filter, status, q);
        }

        // ===== Helpers =====

        private IActionResult BackToIndex(string filter, string status, string? q)
        {
            return RedirectToAction(nameof(Index), new { filter, status, q });
        }

        private static string LabelFor(User user)
        {
            return string.IsNullOrWhiteSpace(user.StudentID) ? user.FullName : user.StudentID;
        }
    }
}