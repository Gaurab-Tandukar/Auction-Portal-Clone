using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class ItemAttachmentDTO
    {
        public int Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public FileType FileType { get; set; }
    }
}