using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostAndFoundSystem.Data;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class MyReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyReportsController(ApplicationDbContext context)
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

            var reports = await _context.Items
                .Include(i => i.Category)
                .Where(i => i.ReportedByUserID == userId)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            return View(reports);
        }
    }
}