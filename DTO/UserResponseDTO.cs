using System.ComponentModel.DataAnnotations;

namespace Auction_Portal_Clone.DTO
{
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? NationalIdNumber { get; set; }
        public bool IsVerifiedForBidding { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}