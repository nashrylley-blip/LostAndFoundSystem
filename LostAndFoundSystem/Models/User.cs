using System;
using System.ComponentModel.DataAnnotations;

namespace LostAndFoundSystem.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? StudentID { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "Student"; // "Student" o "Admin"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}