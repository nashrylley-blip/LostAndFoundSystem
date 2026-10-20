using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public MessagesController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // List student's conversations
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var conversations = await _context.Conversations
                .Include(c => c.Messages)
                .Where(c => c.StudentUserID == userId.Value)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return View(conversations);
        }

        // Start new conversation form
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string subject, string body)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Subject and message are required.";
                return View();
            }

            var conversation = new Conversation
            {
                StudentUserID = userId.Value,
                Subject = subject.Trim(),
                Status = "Open",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            var msg = new ChatMessage
            {
                ConversationID = conversation.ConversationID,
                SenderUserID = userId.Value,
                Body = body.Trim(),
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            // NOTE: No admin notification here anymore.
            // New student messages show up in the admin's envelope (unread messages) instead of the bell.

            TempData["SuccessMessage"] = "Message sent to admin.";
            return RedirectToAction(nameof(Chat), new { id = conversation.ConversationID });
        }

        // Chat view
        [HttpGet]
        public async Task<IActionResult> Chat(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.ConversationID == id && c.StudentUserID == userId.Value);

            if (conversation == null)
                return NotFound();

            // Mark admin messages as read
            var unread = conversation.Messages
                .Where(m => m.SenderUserID != userId.Value && !m.IsRead)
                .ToList();
            foreach (var m in unread)
                m.IsRead = true;
            if (unread.Any())
                await _context.SaveChangesAsync();

            return View(conversation);
        }

        // Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int conversationId, string body)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationID == conversationId && c.StudentUserID == userId.Value);

            if (conversation == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Chat), new { id = conversationId });
            }

            if (conversation.Status == "Closed")
            {
                TempData["ErrorMessage"] = "This conversation is closed.";
                return RedirectToAction(nameof(Chat), new { id = conversationId });
            }

            _context.ChatMessages.Add(new ChatMessage
            {
                ConversationID = conversationId,
                SenderUserID = userId.Value,
                Body = body.Trim(),
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            conversation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // NOTE: No admin notification here anymore (the envelope handles unread student messages).

            return RedirectToAction(nameof(Chat), new { id = conversationId });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}