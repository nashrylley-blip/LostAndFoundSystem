using System.ComponentModel.DataAnnotations;

namespace LostAndFoundSystem.Models
{
    public class VerifyEmailViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit code.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code must be 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }
}
