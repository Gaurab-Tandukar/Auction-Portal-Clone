namespace Auction_Portal_Clone.Models
{
    public enum AuctionStatus
    {
        Draft = 0,
        Upcoming = 1,
        Active = 2,
        Closed = 3
    }
    public enum AuctionFinalStatus
    {
        Pending = 0,
        Sold = 1,
        Unsold = 2
    }

    public class AuctionItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ReservePrice { get; set; }

        // Keep these if you still want map coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime AuctionStartDate { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public AuctionStatus Status { get; set; } = AuctionStatus.Draft;
        public CollateralCategory CollateralCategory { get; set; } = CollateralCategory.Land;

        // Final determination outcome & winner info
        public AuctionFinalStatus FinalStatus { get; set; } = AuctionFinalStatus.Pending;
        public string? WinnerUserId { get; set; }
        public User? WinnerUser { get; set; }
        public int? WinningBidId { get; set; }
        public Bid? WinningBid { get; set; }
        public decimal? WinningAmount { get; set; }
        public DateTime? WinnerDeterminedAt { get; set; }
        public DateTime? WinnerNotifiedAt { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }
        public int MunicipalityId { get; set; }

        // Navigation Properties
        public Category Category { get; set; } = null!;
        public Municipality Municipality { get; set; } = null!;

        public ICollection<ItemAttachment> Attachments { get; set; } = new List<ItemAttachment>();
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public ICollection<SavedListing> SavedListings { get; set; } = new List<SavedListing>();
    }

}