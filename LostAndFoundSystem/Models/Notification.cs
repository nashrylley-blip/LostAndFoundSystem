using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostAndFoundSystem.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "System"; // System, Report, Claim, Message

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? RelatedItemID { get; set; }

        // ===== Recycle Bin (soft delete) =====
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // The admin who moved this notification to the Recycle Bin
        public int? DeletedByUserID { get; set; }
    }
}