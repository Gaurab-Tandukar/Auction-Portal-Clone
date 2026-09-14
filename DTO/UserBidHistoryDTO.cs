using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public enum UserBidOutcome
    {
        ActiveLeading = 0,
        ActiveOutbid = 1,
        Won = 2,
        Lost = 3,
        EndedUnsold = 4
    }

    public class UserBidHistoryItemDTO
    {
        public int BidId { get; set; }
        public int AuctionItemId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal ReservePrice { get; set; }
        public decimal UserBidAmount { get; set; }
        public decimal CurrentHighestBid { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public AuctionStatus AuctionStatus { get; set; }
        public AuctionFinalStatus FinalStatus { get; set; }
        public bool IsUserWinner { get; set; }
        public decimal? WinningAmount { get; set; }
        public UserBidOutcome Outcome { get; set; }
    }
}
