using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class AuctionCatalogService : IAuctionCatalogService
    {
        private readonly AuctionDbContext _db;
        private readonly IAuctionFilterService _filterService;

        public AuctionCatalogService(AuctionDbContext db, IAuctionFilterService filterService)
        {
            _db = db;
            _filterService = filterService;
        }

        public async Task<PagedResultDTO<AuctionItemListItemDTO>> GetCatalogAsync(AuctionItemFilterDTO filter)
        {
            var query = _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Attachments)
                .AsQueryable();

            query = _filterService.ApplyFilters(query, filter);

            // Never show Draft items on the public catalog. This stays here
            // (not in AuctionFilterService) because it's a rule specific to
            // the public-facing catalog, not a user-controlled filter.
            query = query.Where(a => a.Status != AuctionStatus.Draft);

            var totalCount = await query.CountAsync();

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 12 : filter.PageSize;

            var rawItems = await query
                .OrderBy(a => a.AuctionEndDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    MunicipalityName = a.Municipality.Name,
                    DistrictName = a.Municipality.District.Name,
                    a.ReservePrice,
                    a.AuctionStartDate,
                    a.AuctionEndDate,
                    a.Status,
                    a.CollateralCategory,
                    CategoryName = a.Category.Name,
                    ThumbnailUrl = a.Attachments
                        .Where(att => att.FileType == FileType.Image)
                        .Select(att => att.FileUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var items = rawItems.Select(a => new AuctionItemListItemDTO
            {
                Id = a.Id,
                Title = a.Title,
                MunicipalityName = a.MunicipalityName,
                DistrictName = a.DistrictName,
                ReservePrice = a.ReservePrice,
                AuctionStartDate = a.AuctionStartDate,
                AuctionEndDate = a.AuctionEndDate,
                Status = AuctionStatusResolver.Resolve(a.Status, a.AuctionStartDate, a.AuctionEndDate),
                CollateralCategory = a.CollateralCategory,
                CollateralCategoryName = a.CollateralCategory.ToDisplayName(),
                CategoryName = a.CategoryName,
                ThumbnailUrl = a.ThumbnailUrl
            }).ToList();

            return new PagedResultDTO<AuctionItemListItemDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuctionItemDetailDTO?> GetDetailAsync(int id, string? currentUserId)
        {
            var item = await _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Attachments)
                .Include(a => a.Municipality).ThenInclude(m => m.District).ThenInclude(d => d.Province)
                .Include(a => a.WinnerUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (item is null)
                return null;

            bool isSaved = false;
            bool hasBid = false;
            decimal? userHighestBid = null;

            if (!string.IsNullOrEmpty(currentUserId))
            {
                isSaved = await _db.SavedListings
                    .AnyAsync(s => s.AuctionItemId == id && s.UserId == currentUserId);

                var userBids = await _db.Bids
                    .Where(b => b.AuctionItemId == id && b.UserId == currentUserId)
                    .Select(b => b.OfferedAmount)
                    .ToListAsync();

                if (userBids.Count > 0)
                {
                    hasBid = true;
                    userHighestBid = userBids.Max();
                }
            }

            bool isWinner = !string.IsNullOrEmpty(currentUserId) &&
                            item.FinalStatus == AuctionFinalStatus.Sold &&
                            item.WinnerUserId == currentUserId;

            return new AuctionItemDetailDTO
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ReservePrice = item.ReservePrice,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AuctionStartDate = item.AuctionStartDate,
                AuctionEndDate = item.AuctionEndDate,
                Status = AuctionStatusResolver.Resolve(item.Status, item.AuctionStartDate, item.AuctionEndDate),
                CollateralCategory = item.CollateralCategory,
                CollateralCategoryName = item.CollateralCategory.ToDisplayName(),
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
                MunicipalityId = item.MunicipalityId,
                MunicipalityName = item.Municipality.Name,
                DistrictId = item.Municipality.DistrictId,
                DistrictName = item.Municipality.District.Name,
                ProvinceId = item.Municipality.District.ProvinceId,
                ProvinceName = item.Municipality.District.Province.Name,
                ImageUrls = item.Attachments
                    .Where(a => a.FileType == FileType.Image)
                    .Select(a => a.FileUrl)
                    .ToList(),
                DocumentUrls = item.Attachments
                    .Where(a => a.FileType == FileType.PDFNotice)
                    .Select(a => new AttachmentDocumentDTO
                    {
                        Id = a.Id,
                        FileUrl = a.FileUrl,
                        FileName = a.FileName
                    })
                    .ToList(),
                IsSavedByCurrentUser = isSaved,
                HasCurrentUserBid = hasBid,
                CurrentUserHighestBid = userHighestBid,
                FinalStatus = item.FinalStatus,
                WinningAmount = item.WinningAmount,
                WinnerDeterminedAt = item.WinnerDeterminedAt,
                IsCurrentUserWinner = isWinner,
                WinnerDisplayName = item.WinnerUser?.FullName
            };
        }
    }
}