using Microsoft.VisualBasic.FileIO;

namespace Auction_Portal_Clone.Models
{

    public enum FileType
    {
        Image = 0,
        PDFNotice = 1
    }

    public class ItemAttachment
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public string? FileName { get; set; }

        // Foreign Key
        public int AuctionItemId { get; set; }

        // Navigation Property
        public AuctionItem AuctionItem { get; set; } = null!;
    }
}