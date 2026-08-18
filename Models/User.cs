using Microsoft.AspNetCore.Identity;

namespace Auction_Portal_Clone.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsVerifiedForBidding { get; set; } = false;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();

        public ICollection<SavedListing> SavedListings { get; set; } = new List<SavedListing>();
    }
}