using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AuctionItemListItemDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal ReservePrice { get; set; }
        public DateTime AuctionStartDate { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public AuctionStatus Status { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string MunicipalityName { get; set; } = string.Empty;
        public string DistrictName { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }
}