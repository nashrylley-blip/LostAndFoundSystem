using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Helpers;
using LostAndFoundSystem.Services;

namespace LostAndFoundSystem.Controllers
{
    public class AccountController : Controller
    {
        private const int CodeLifetimeMinutes = 10;
        private const int MaxAttempts = 5;
        private const int ResendCooldownSeconds = 60;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;

        public AccountController(
            ApplicationDbContext context,
            ILogger<AccountController> logger,
            IMemoryCache cache,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _emailService = emailService;
        }

        private static string PendingKey(string email)
        {
            return "pending-registration:" + email;
        }

        // ================================
        // LOGIN PAGE
        // ================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ================================
        // PROCESS LOGIN
        // Students log in with their Student ID.
        // Admins log in with their admin email or username.
        // ================================

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) ||
                string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.Role))
            {
                ViewBag.Error = "Please complete all fields.";
                return View(model);
            }

            string username = model.Username.Trim();

            var query = _context.Users.Where(u => u.Role == model.Role);

            if (model.Role == "Admin")
            {
                query = query.Where(u => u.Email == username || u.FullName == username);
            }
            else
            {
                query = query.Where(u => u.StudentID == username);
            }

            var candidates = await query.ToListAsync();

            // Check the password (supports hashed AND old plain-text passwords)
            var user = candidates.FirstOrDefault(u =>
                PasswordHelper.Verify(model.Password, u.PasswordHash));

            if (user == null)
            {
                ViewBag.Error = model.Role == "Admin"
                    ? "Invalid username, password, or account type."
                    : "Invalid Student ID, password, or account type.";
                return View(model);
            }

            // Deactivated accounts cannot log in (checked after the password,
            // so the message is only shown to someone who knows the password)
            if (user.IsDeactivated)
            {
                ViewBag.Error = "This account has been deactivated. Please contact the administrator.";
                return View(model);
            }

            // Upgrade old plain-text passwords to a secure hash
            if (!PasswordHelper.IsHashed(user.PasswordHash))
            {
                try
                {
                    user.PasswordHash = PasswordHelper.Hash(model.Password);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not upgrade the password hash for user {UserId}.", user.UserID);
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        // ================================
        // REGISTER PAGE
        // ================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // ================================
        // STEP 1: VALIDATE FORM + SEND CODE TO GMAIL
        // ================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Clean the input first (mobile keyboards often add spaces / capital letters)
            model.StudentID = (model.StudentID ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            // Re-validate using the cleaned values
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Duplicate checks
            if (await _context.Users.AnyAsync(u => u.StudentID == model.StudentID))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.StudentID),
                    "This Student ID is already registered.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email),
                    "This Gmail is already registered.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The SMTP account must be set up in appsettings
            if (!_emailService.IsConfigured)
            {
                _logger.LogError("SMTP is not configured. Fill in Smtp:Username and Smtp:AppPassword in appsettings.Development.json.");

                ModelState.AddModelError(string.Empty,
                    "The email service is not set up yet. Please contact the administrator.");

                return View(model);
            }

            var pending = new PendingRegistration
            {
                StudentID = model.StudentID,
                Email = model.Email,
                PasswordHash = PasswordHelper.Hash(model.Password),
                Code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(),
                Attempts = 0,
                LastSentAt = DateTime.Now
            };

            try
            {
                await SendVerificationEmailAsync(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send the verification email to {Email}.", pending.Email);

                ModelState.AddModelError(nameof(RegisterViewModel.Email),
                    "We could not send the verification code. Please check your Gmail address and try again.");

                return View(model);
            }

            _cache.Set(PendingKey(pending.Email), pending, TimeSpan.FromMinutes(CodeLifetimeMinutes));

            return RedirectToAction("VerifyEmail", "Account", new { email = pending.Email });
        }

        // ================================
        // STEP 2: ENTER THE CODE
        // ================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyEmail(string? email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (!_cache.TryGetValue(PendingKey(email), out PendingRegistration? pending) || pending == null)
            {
                TempData["Error"] = "Your verification session expired. Please create your account again.";
                return RedirectToAction("Register", "Account");
            }

            return View(new VerifyEmailViewModel { Email = email });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            string email = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            model.Email = email;
            model.Code = (model.Code ?? string.Empty).Trim();

            ModelState.Clear();
            TryValidateModel(model);

            if (!_cache.TryGetValue(PendingKey(email), out PendingRegistration? pending) || pending == null)
            {
                TempData["Error"] = "Your code expired. Please create your account again.";
                return RedirectToAction("Register", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (pending.Attempts >= MaxAttempts)
            {
                _cache.Remove(PendingKey(email));
                TempData["Error"] = "Too many wrong attempts. Please create your account again.";
                return RedirectToAction("Register", "Account");
            }

            bool codeMatches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(model.Code),
                Encoding.UTF8.GetBytes(pending.Code));

            if (!codeMatches)
            {
                pending.Attempts++;

                int left = MaxAttempts - pending.Attempts;

                ModelState.AddModelError(nameof(VerifyEmailViewModel.Code),
                    left > 0
                        ? "Incorrect code. You have " + left + " attempt(s) left."
                        : "Incorrect code. No attempts left.");

                if (left <= 0)
                {
                    _cache.Remove(PendingKey(email));
                    TempData["Error"] = "Too many wrong attempts. Please create your account again.";
                    return RedirectToAction("Register", "Account");
                }

                return View(model);
            }

            // Code is correct - check duplicates one last time
            if (await _context.Users.AnyAsync(u => u.StudentID == pending.StudentID || u.Email == pending.Email))
            {
                _cache.Remove(PendingKey(email));
                TempData["Error"] = "This Student ID or Gmail is already registered.";
                return RedirectToAction("Register", "Account");
            }

            var newUser = new User
            {
                FullName = pending.StudentID,   // no name field in the form, so use the Student ID
                Email = pending.Email,
                StudentID = pending.StudentID,
                PasswordHash = pending.PasswordHash,
                Role = "Student",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to create account for Student ID {StudentId}.", pending.StudentID);

                ModelState.AddModelError(string.Empty,
                    "We could not create your account right now. Please try again.");

                return View(model);
            }

            _cache.Remove(PendingKey(email));

            TempData["Success"] = "Email verified! Your account is ready. Sign in with your Student ID.";

            return RedirectToAction("Login", "Account");
        }

        // ================================
        // RESEND CODE
        // ================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode(string? email)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();

            if (!_cache.TryGetValue(PendingKey(email), out PendingRegistration? pending) || pending == null)
            {
                TempData["Error"] = "Your verification session expired. Please create your account again.";
                return RedirectToAction("Register", "Account");
            }

            var model = new VerifyEmailViewModel { Email = email };

            double secondsSinceLast = (DateTime.Now - pending.LastSentAt).TotalSeconds;

            if (secondsSinceLast < ResendCooldownSeconds)
            {
                int wait = (int)Math.Ceiling(ResendCooldownSeconds - secondsSinceLast);
                ModelState.AddModelError(string.Empty,
                    "Please wait " + wait + " second(s) before requesting a new code.");
                return View("VerifyEmail", model);
            }

            pending.Code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            pending.Attempts = 0;
            pending.LastSentAt = DateTime.Now;

            try
            {
                await SendVerificationEmailAsync(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not resend the verification email to {Email}.", pending.Email);

                ModelState.AddModelError(string.Empty,
                    "We could not send the code. Please try again in a moment.");

                return View("VerifyEmail", model);
            }

            _cache.Set(PendingKey(pending.Email), pending, TimeSpan.FromMinutes(CodeLifetimeMinutes));

            ViewBag.Info = "A new code was sent to your Gmail.";

            return View("VerifyEmail", model);
        }

        // ================================
        // LOGOUT
        // ================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login", "Account");
        }

        // ================================
        // ACCESS DENIED
        // ================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ================================
        // HELPER: SEND THE CODE EMAIL
        // ================================

        private async Task SendVerificationEmailAsync(PendingRegistration pending)
        {
            string body =
                "<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:420px;margin:auto;padding:24px;border:1px solid #e5eef3;border-radius:14px\">" +
                "<h2 style=\"color:#174b73;margin-top:0\">Campus Lost &amp; Found</h2>" +
                "<p>Use this code to verify your Gmail and finish creating your account:</p>" +
                "<p style=\"font-size:32px;font-weight:700;letter-spacing:8px;color:#087ea4;margin:18px 0\">" + pending.Code + "</p>" +
                "<p style=\"color:#6b7f8c;font-size:13px\">This code expires in " + CodeLifetimeMinutes + " minutes. " +
                "If you did not request it, you can ignore this email.</p>" +
                "</div>";

            await _emailService.SendAsync(
                pending.Email,
                "Your Campus Lost & Found verification code",
                body);
        }
    }
}