namespace Auction_Portal_Clone.Models
{
    public class Municipality
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Foreign Key
        public int DistrictId { get; set; }

        // Navigation
        public District District { get; set; } = null!;
        public ICollection<AuctionItem> AuctionItems { get; set; } = new List<AuctionItem>();
    }
}