using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Get the currently logged-in user's ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return View(new DashboardViewModel());
            }

            int userId = int.Parse(userIdClaim.Value);

            // Get only the reports submitted by the logged-in user
            var userReports = _context.Items
                .Where(i => i.ReportedByUserID == userId)
                .ToList();

            var dashboard = new DashboardViewModel
            {
                LostItems = userReports.Count(i =>
                    i.Status.ToLower() == "lost"),

                FoundItems = userReports.Count(i =>
                    i.Status.ToLower() == "found"),

                ResolvedItems = userReports.Count(i =>
                    i.Status.ToLower() == "claimed")
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}