using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AuctionWinnerResultDTO
    {
        public int AuctionItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public AuctionFinalStatus FinalStatus { get; set; }
        public string? WinnerUserId { get; set; }
        public string? WinnerName { get; set; }
        public string? WinnerEmail { get; set; }
        public decimal? WinningAmount { get; set; }
        public int? WinningBidId { get; set; }
        public bool IsWinnerFound => FinalStatus == AuctionFinalStatus.Sold && !string.IsNullOrEmpty(WinnerUserId);
        public bool EmailSent { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AuctionWinnerProcessResultDTO
    {
        public int TotalChecked { get; set; }
        public int TotalSold { get; set; }
        public int TotalUnsold { get; set; }
        public int TotalErrors { get; set; }
        public int TotalEmailsSent { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public List<AuctionWinnerResultDTO> Results { get; set; } = new();
    }
}
