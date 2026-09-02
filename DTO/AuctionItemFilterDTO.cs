using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AuctionItemFilterDTO
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public CollateralCategory? CollateralCategory { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? MunicipalityId { get; set; }
        public List<int> CategoryIds { get; set; } = new();
        public List<CollateralCategory> CollateralCategories { get; set; } = new();
        public List<int> ProvinceIds { get; set; } = new();
        public List<int> DistrictIds { get; set; } = new();
        public List<int> MunicipalityIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? AuctionDateFrom { get; set; }
        public DateTime? AuctionDateTo { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}