using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSettingsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
