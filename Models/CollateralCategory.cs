using System.ComponentModel.DataAnnotations;

namespace Auction_Portal_Clone.Models
{
    public enum CollateralCategory
    {
        [Display(Name = "Land")]
        Land = 1,

        [Display(Name = "Residential Property")]
        ResidentialProperty = 2,

        [Display(Name = "Commercial")]
        Commercial = 3,

        [Display(Name = "Vehicle")]
        Vehicle = 4
    }

    public static class CollateralCategoryExtensions
    {
        public static string ToDisplayName(this CollateralCategory category)
        {
            return category switch
            {
                CollateralCategory.Land => "Land",
                CollateralCategory.ResidentialProperty => "Residential Property",
                CollateralCategory.Commercial => "Commercial",
                CollateralCategory.Vehicle => "Vehicle",
                _ => category.ToString()
            };
        }

        public static CollateralCategory? ParseCollateralCategory(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var normalized = input.Trim().Replace(" ", "").Replace("-", "").Replace("_", "");

            if (Enum.TryParse<CollateralCategory>(normalized, true, out var result))
                return result;

            if (int.TryParse(input, out var intVal) && Enum.IsDefined(typeof(CollateralCategory), intVal))
                return (CollateralCategory)intVal;

            return null;
        }
    }
}
