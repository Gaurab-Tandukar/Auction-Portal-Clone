using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    /// <summary>
    /// Shared query-filtering logic for AuctionItem listings, used by both
    /// the public catalog (AuctionCatalogService) and the admin listing
    /// (AdminAuctionItemService). Keeps filter behavior consistent and in
    /// one place instead of duplicated across the two services.
    /// </summary>
    public class AuctionFilterService : IAuctionFilterService
    {
        public IQueryable<AuctionItem> ApplyFilters(IQueryable<AuctionItem> query, AuctionItemFilterDTO filter)
        {
            if (filter.CategoryId.HasValue)
                query = query.Where(a => a.CategoryId == filter.CategoryId.Value);
            if (filter.CategoryIds.Count > 0)
                query = query.Where(a => filter.CategoryIds.Contains(a.CategoryId));

            if (filter.CollateralCategory.HasValue)
                query = query.Where(a => a.CollateralCategory == filter.CollateralCategory.Value);
            if (filter.CollateralCategories.Count > 0)
                query = query.Where(a => filter.CollateralCategories.Contains(a.CollateralCategory));

            if (filter.ProvinceId.HasValue)
                query = query.Where(a => a.Municipality.District.ProvinceId == filter.ProvinceId.Value);
            if (filter.ProvinceIds.Count > 0)
                query = query.Where(a => filter.ProvinceIds.Contains(a.Municipality.District.ProvinceId));

            if (filter.DistrictId.HasValue)
                query = query.Where(a => a.Municipality.DistrictId == filter.DistrictId.Value);
            if (filter.DistrictIds.Count > 0)
                query = query.Where(a => filter.DistrictIds.Contains(a.Municipality.DistrictId));

            if (filter.MunicipalityId.HasValue)
                query = query.Where(a => a.MunicipalityId == filter.MunicipalityId.Value);
            if (filter.MunicipalityIds.Count > 0)
                query = query.Where(a => filter.MunicipalityIds.Contains(a.MunicipalityId));

            if (filter.MinPrice.HasValue)
                query = query.Where(a => a.ReservePrice >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(a => a.ReservePrice <= filter.MaxPrice.Value);

            if (filter.AuctionDateFrom.HasValue)
                query = query.Where(a => a.AuctionStartDate >= filter.AuctionDateFrom.Value);

            if (filter.AuctionDateTo.HasValue)
                query = query.Where(a => a.AuctionEndDate <= filter.AuctionDateTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();

                // Purely numeric terms also get checked as an exact Id match,
                // in addition to the text search below.
                bool isNumeric = int.TryParse(term, out var idTerm);
                var matchedCollateralCategory = CollateralCategoryExtensions.ParseCollateralCategory(term);

                query = query.Where(a =>
                    (isNumeric && a.Id == idTerm) ||
                    (matchedCollateralCategory.HasValue && a.CollateralCategory == matchedCollateralCategory.Value) ||
                    EF.Functions.Like(a.Title, $"%{term}%") ||
                    (a.Description != null && EF.Functions.Like(a.Description, $"%{term}%")) ||
                    EF.Functions.Like(a.Category.Name, $"%{term}%") ||
                    EF.Functions.Like(a.Municipality.Name, $"%{term}%") ||
                    EF.Functions.Like(a.Municipality.District.Name, $"%{term}%") ||
                    EF.Functions.Like(a.Status.ToString(), $"%{term}%"));
            }

            return query;
        }
    }
}