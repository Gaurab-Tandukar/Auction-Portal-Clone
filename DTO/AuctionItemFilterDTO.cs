namespace Auction_Portal_Clone.DTO
{
    public class AuctionItemFilterDTO
    {
        public int? CategoryId { get; set; }
        public string? City { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? AuctionDateFrom { get; set; }
        public DateTime? AuctionDateTo { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}