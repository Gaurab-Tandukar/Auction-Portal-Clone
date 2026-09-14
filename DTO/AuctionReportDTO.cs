using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AuctionReportFilterDTO
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public AuctionFinalStatus? FinalStatus { get; set; }
        public int? CategoryId { get; set; }
        public CollateralCategory? CollateralCategory { get; set; }
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class AuctionReportItemDTO
    {
        public int AuctionItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public CollateralCategory CollateralCategory { get; set; }
        public string CollateralCategoryName { get; set; } = string.Empty;
        public string MunicipalityName { get; set; } = string.Empty;
        public string DistrictName { get; set; } = string.Empty;
        public decimal ReservePrice { get; set; }
        public DateTime AuctionStartDate { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public AuctionStatus Status { get; set; }
        public AuctionFinalStatus FinalStatus { get; set; }

        public decimal? WinningAmount { get; set; }
        public string? WinnerUserId { get; set; }
        public string? WinnerName { get; set; }
        public string? WinnerEmail { get; set; }
        public string? WinnerPhoneNumber { get; set; }
        public DateTime? WinnerDeterminedAt { get; set; }
        public DateTime? WinnerNotifiedAt { get; set; }

        public int TotalBids { get; set; }
        public decimal? HighestBidAmount { get; set; }
    }

    public class AuctionReportSummaryDTO
    {
        public int TotalEndedAuctions { get; set; }
        public int TotalSoldAuctions { get; set; }
        public int TotalUnsoldAuctions { get; set; }
        public int TotalPendingAuctions { get; set; }
        public decimal TotalWinningValue { get; set; }
        public int TotalBidsCount { get; set; }
    }

    public class AuctionReportViewModel
    {
        public AuctionReportSummaryDTO Summary { get; set; } = new();
        public PagedResultDTO<AuctionReportItemDTO> PagedItems { get; set; } = new();
        public AuctionReportFilterDTO Filter { get; set; } = new();
    }
}
