namespace Auction_Portal_Clone.DTO
{
    public class BidDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public decimal OfferedAmount { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class PlaceBidDTO
    {
        public int AuctionItemId { get; set; }
        public decimal OfferedAmount { get; set; }
    }
}