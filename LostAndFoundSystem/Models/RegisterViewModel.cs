using System.ComponentModel.DataAnnotations;

namespace LostAndFoundSystem.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Student ID is required.")]
        [RegularExpression(@"^\d{4}-\d{4,6}$",
            ErrorMessage = "Student ID must look like 2026-12345.")]
        [Display(Name = "Student ID")]
        public string StudentID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gmail is required.")]
        [StringLength(100, ErrorMessage = "Gmail is too long.")]
        [RegularExpression(@"^[a-z0-9._%+\-]+@gmail\.com$",
            ErrorMessage = "Please enter a valid Gmail address (example@gmail.com).")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one letter and one number.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
