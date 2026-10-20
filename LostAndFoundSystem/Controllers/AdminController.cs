using LostAndFoundSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.PendingReports = await _context.Items
                .CountAsync(i => i.ApprovalStatus == "Pending");

            ViewBag.ApprovedItems = await _context.Items
                .CountAsync(i => i.ApprovalStatus == "Approved");

            ViewBag.TotalUsers = await _context.Users.CountAsync();

            ViewBag.PendingClaims = await _context.Claims
                .CountAsync(c => c.Status == "Pending");

            ViewBag.LostItems = await _context.Items
                .CountAsync(i => i.Status == "Lost" && i.ApprovalStatus == "Approved");

            ViewBag.FoundItems = await _context.Items
                .CountAsync(i => i.Status == "Found" && i.ApprovalStatus == "Approved");

            return View();
        }
    }
}
