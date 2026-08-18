using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AdminAuctionItemCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ReservePrice { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime AuctionStartDate { get; set; }
        public DateTime AuctionEndDate { get; set; }
        public AuctionStatus Status { get; set; } = AuctionStatus.Draft;
        public int CategoryId { get; set; }
    }

    public class AdminAuctionItemUpdateDTO : AdminAuctionItemCreateDTO
    {
        public int Id { get; set; }
    }
}