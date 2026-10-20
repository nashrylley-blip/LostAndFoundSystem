using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostAndFoundSystem.Models
{
    [Table("Items")]
    public class Item
    {
        [Key]
        [Column("ItemId")]
        public int ItemID { get; set; }

        [Required]
        [Column("ItemName")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column("CategoryId")]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public Category? Category { get; set; }

        [Column("LocationFoundOrLost")]
        [StringLength(100)]
        public string LocationFound { get; set; } = string.Empty;

        [Column("DateReported")]
        public DateTime DateReported { get; set; } = DateTime.Now;

        [Column("Status")]
        [StringLength(20)]
        public string Status { get; set; } = "Lost"; // Lost, Found, Claimed

        [Required]
        [Column("ApprovalStatus")]
        [StringLength(20)]
        public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        [Column("ImageUrl")]
        public string? ImageUrl { get; set; }

        [Column("UserId")]
        public int ReportedByUserID { get; set; }

        [ForeignKey("ReportedByUserID")]
        public User? ReportedByUser { get; set; }

        public string? RejectionReason { get; set; }

        // ===== Recycle Bin (soft delete) =====
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // The admin who moved this item to the Recycle Bin
        public int? DeletedByUserID { get; set; }
    }
}