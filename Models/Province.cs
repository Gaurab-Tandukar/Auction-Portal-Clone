namespace Auction_Portal_Clone.Models
{
    public class Province
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation
        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}