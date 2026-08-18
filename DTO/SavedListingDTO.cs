namespace Auction_Portal_Clone.DTO
{
    public class SavedListingDTO
    {
        public int AuctionItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal ReservePrice { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public DateTime SavedAt { get; set; }
    }
}