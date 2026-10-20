using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostAndFoundSystem.Models
{
    public class Conversation
    {
        [Key]
        public int ConversationID { get; set; }

        [Required]
        public int StudentUserID { get; set; }

        [ForeignKey("StudentUserID")]
        public User? Student { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, Closed

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
