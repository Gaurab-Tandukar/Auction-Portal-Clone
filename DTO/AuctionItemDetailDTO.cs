using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AuctionItemDetailDTO
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
        public AuctionStatus Status { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public List<string> ImageUrls { get; set; } = new();
        public List<AttachmentDocumentDTO> DocumentUrls { get; set; } = new();

        public bool IsSavedByCurrentUser { get; set; }
        public bool HasCurrentUserBid { get; set; }
    }

    public class AttachmentDocumentDTO
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
    }
}