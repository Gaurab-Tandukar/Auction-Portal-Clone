namespace Auction_Portal_Clone.Models
{
    public class District
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Foreign Key
        public int ProvinceId { get; set; }

        // Navigation
        public Province Province { get; set; } = null!;
        public ICollection<Municipality> Municipalities { get; set; } = new List<Municipality>();
    }
}