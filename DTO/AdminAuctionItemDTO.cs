using System.ComponentModel.DataAnnotations;
using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.DTO
{
    public class AdminAuctionItemCreateDTO
    {
        [Display(Name = "Title")]
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Reserve Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Reserve price must be positive")]
        public decimal ReservePrice { get; set; }

        [Display(Name = "Latitude")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Display(Name = "Longitude")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [Display(Name = "Auction Start Date")]
        [Required(ErrorMessage = "Auction start date is required")]
        public DateTime AuctionStartDate { get; set; }

        [Display(Name = "Auction End Date")]
        [Required(ErrorMessage = "Auction end date is required")]
        public DateTime AuctionEndDate { get; set; }

        [Display(Name = "Status")]
        public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

        [Display(Name = "Collateral Category")]
        public CollateralCategory CollateralCategory { get; set; } = CollateralCategory.Land;

        [Display(Name = "Auction Category")]
        [Required(ErrorMessage = "Please select a category")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid auction category")]
        public int CategoryId { get; set; }

        [Display(Name = "Municipality")]
        [Required(ErrorMessage = "Please select a municipality")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid municipality")]
        public int MunicipalityId { get; set; }
    }

    public class AdminAuctionItemUpdateDTO : AdminAuctionItemCreateDTO
    {
        public int Id { get; set; }
    }
}