namespace Auction_Portal_Clone.Models
{
    public class Bid
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal OfferedAmount { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int AuctionItemId { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public AuctionItem AuctionItem { get; set; } = null!;
    }
}