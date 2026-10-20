using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
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
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction(nameof(Index));
            }

            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "New password must be at least 6 characters.";
                return RedirectToAction(nameof(Index));
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Index));
            }

            if (!PasswordHelper.Verify(currentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            user.PasswordHash = PasswordHelper.Hash(newPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string email)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Full name and email are required.";
                return RedirectToAction(nameof(Index));
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == email.Trim() && u.UserID != userId);

            if (emailExists)
            {
                TempData["ErrorMessage"] = "That email is already used by another account.";
                return RedirectToAction(nameof(Index));
            }

            user.FullName = fullName.Trim();
            user.Email = email.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
