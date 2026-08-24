using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AdminDashboardSummaryDTO
    {
        // Item counts by status
        public int DraftCount { get; set; }
        public int UpcomingCount { get; set; }
        public int ActiveCount { get; set; }
        public int ClosedCount { get; set; }

        // Health flags
        public int ItemsEndingSoon { get; set; }        // Active, ending within 24-48h
        public int ItemsPastEndButActive { get; set; }  // Status = Active but AuctionEndDate < now
        public int ItemsMissingImages { get; set; }
        public int ItemsMissingPdfNotice { get; set; }
        public int ItemsBelowReserve { get; set; }       // highest bid < reserve, auction ended

        // Bid activity
        public int TotalBidsToday { get; set; }
        public decimal TotalBidValueToday { get; set; }
        public List<RecentBidDTO> RecentBids { get; set; } = new();

        // User stats
        public int TotalUsers { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int VerifiedBidders { get; set; }

        // Recent activity
        public List<RecentAuctionItemDTO> RecentlyCreatedItems { get; set; } = new();

        // Category breakdown
        public List<CategoryCountDTO> ItemsByCategory { get; set; } = new();
    }

    public class RecentBidDTO
    {
        public int AuctionItemId { get; set; }
        public string AuctionItemTitle { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public decimal OfferedAmount { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class RecentAuctionItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public AuctionStatus Status { get; set; }
        public DateTime AuctionEndDate { get; set; }
    }

    public class CategoryCountDTO
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }
}