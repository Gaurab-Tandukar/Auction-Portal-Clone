using System.ComponentModel.DataAnnotations;

namespace Auction_Portal_Clone.DTO
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string? NationalIdNumber { get; set; }

        // Set to true if creating a Bank Admin account (can be restricted in production)
        public bool IsAdminRegistration { get; set; } = false;
    }
}