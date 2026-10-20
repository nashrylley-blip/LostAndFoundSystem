using System.Security.Claims;
using LostAndFoundSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Stats for student
            if (user.Role == "Student")
            {
                var reports = await _context.Items
                    .Where(i => i.ReportedByUserID == userId)
                    .ToListAsync();

                ViewBag.TotalReports = reports.Count;
                ViewBag.PendingReports = reports.Count(r => r.ApprovalStatus == "Pending");
                ViewBag.ApprovedReports = reports.Count(r => r.ApprovalStatus == "Approved");
                ViewBag.ClaimedItems = reports.Count(r => r.Status == "Claimed");
            }

            return View(user);
        }
    }
}
