using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostAndFoundSystem.Models
{
    public class ChatMessage
    {
        [Key]
        public int ChatMessageID { get; set; }

        [Required]
        public int ConversationID { get; set; }

        [ForeignKey("ConversationID")]
        public Conversation? Conversation { get; set; }

        [Required]
        public int SenderUserID { get; set; }

        [ForeignKey("SenderUserID")]
        public User? Sender { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
