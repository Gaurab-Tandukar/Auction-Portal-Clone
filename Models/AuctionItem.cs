namespace Auction_Portal_Clone.Models
{
    public enum AuctionStatus
    {
        Draft = 0,
        Upcoming = 1,
        Active = 2,
        Closed = 3
    }
    public class AuctionItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ReservePrice { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime AuctionStartDate { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

        // Foreign Key
        public int CategoryId { get; set; }

        // Navigation Properties
        public Category Category { get; set; } = null!;
        public ICollection<ItemAttachment> Attachments { get; set; } = new List<ItemAttachment>();
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public ICollection<SavedListing> SavedListings { get; set; } = new List<SavedListing>();
    }
}