using System.Security.Claims;
using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using LostAndFoundSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;

        public AdminMessagesController(ApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filter = "All")
        {
            var query = _context.Conversations
                .Include(c => c.Student)
                .Include(c => c.Messages)
                .AsQueryable();

            if (filter == "Open")
                query = query.Where(c => c.Status == "Open");
            else if (filter == "Closed")
                query = query.Where(c => c.Status == "Closed");

            var conversations = await query
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            ViewBag.Filter = filter;
            return View(conversations);
        }

        [HttpGet]
        public async Task<IActionResult> Chat(int id)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Student)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.ConversationID == id);

            if (conversation == null)
                return NotFound();

            var adminId = GetUserId();
            // Mark student messages as read
            var unread = conversation.Messages
                .Where(m => m.SenderUserID == conversation.StudentUserID && !m.IsRead)
                .ToList();
            foreach (var m in unread)
                m.IsRead = true;
            if (unread.Any())
                await _context.SaveChangesAsync();

            return View(conversation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int conversationId, string body)
        {
            var adminId = GetUserId();
            if (adminId == null) return Unauthorized();

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationID == conversationId);

            if (conversation == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Chat), new { id = conversationId });
            }

            _context.ChatMessages.Add(new ChatMessage
            {
                ConversationID = conversationId,
                SenderUserID = adminId.Value,
                Body = body.Trim(),
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            conversation.UpdatedAt = DateTime.Now;
            if (conversation.Status == "Closed")
                conversation.Status = "Open";

            await _context.SaveChangesAsync();

            // Notify the student
            await _notifications.NotifyAsync(
                conversation.StudentUserID,
                "New reply from Admin",
                $"Admin replied in: \"{conversation.Subject}\"",
                "Message");

            return RedirectToAction(nameof(Chat), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
                return NotFound();

            conversation.Status = "Closed";
            conversation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(
                conversation.StudentUserID,
                "Conversation closed",
                $"Admin closed the conversation: \"{conversation.Subject}\"",
                "Message");

            TempData["SuccessMessage"] = "Conversation closed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
                return NotFound();

            conversation.Status = "Open";
            conversation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Conversation reopened.";
            return RedirectToAction(nameof(Chat), new { id });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}
