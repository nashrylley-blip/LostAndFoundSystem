using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostAndFoundSystem.Models
{
    public class ItemClaim
    {
        [Key]
        public int ClaimID { get; set; }

        [Required]
        public int ItemID { get; set; }

        [ForeignKey("ItemID")]
        public Item? Item { get; set; }

        [Required]
        public int ClaimedByUserID { get; set; }

        [ForeignKey("ClaimedByUserID")]
        public User? ClaimedByUser { get; set; }

        public string ProofOfOwnership { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

        public DateTime DateClaimed { get; set; } = DateTime.Now;
    }
}