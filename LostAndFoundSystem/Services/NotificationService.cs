using LostAndFoundSystem.Data;
using LostAndFoundSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LostAndFoundSystem.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(int userId, string title, string message, string type = "System", int? relatedItemId = null);
        Task NotifyAdminsAsync(string title, string message, string type = "System", int? relatedItemId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task NotifyAsync(int userId, string title, string message, string type = "System", int? relatedItemId = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserID = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedItemID = relatedItemId,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task NotifyAdminsAsync(string title, string message, string type = "System", int? relatedItemId = null)
        {
            var adminIds = await _context.Users
                .Where(u => u.Role == "Admin")
                .Select(u => u.UserID)
                .ToListAsync();

            foreach (var adminId in adminIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserID = adminId,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedItemID = relatedItemId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
