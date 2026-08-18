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

        public AuctionCatalogService(AuctionDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResultDTO<AuctionItemListItemDTO>> GetCatalogAsync(AuctionItemFilterDTO filter)
        {
            var query = _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Attachments)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
                query = query.Where(a => a.CategoryId == filter.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(a => a.Location.Contains(filter.City));

            if (filter.MinPrice.HasValue)
                query = query.Where(a => a.ReservePrice >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(a => a.ReservePrice <= filter.MaxPrice.Value);

            if (filter.AuctionDateFrom.HasValue)
                query = query.Where(a => a.AuctionStartDate >= filter.AuctionDateFrom.Value);

            if (filter.AuctionDateTo.HasValue)
                query = query.Where(a => a.AuctionEndDate <= filter.AuctionDateTo.Value);

            // Never show Draft items on the public catalog
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
        a.Location,
        a.ReservePrice,
        a.AuctionStartDate,
        a.AuctionEndDate,
        a.Status,
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
                Location = a.Location,
                ReservePrice = a.ReservePrice,
                AuctionStartDate = a.AuctionStartDate,
                AuctionEndDate = a.AuctionEndDate,
                Status = AuctionStatusResolver.Resolve(a.Status, a.AuctionStartDate, a.AuctionEndDate),
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
                .FirstOrDefaultAsync(a => a.Id == id);

            if (item is null)
                return null;

            bool isSaved = false;
            bool hasBid = false;

            if (!string.IsNullOrEmpty(currentUserId))
            {
                isSaved = await _db.SavedListings
                    .AnyAsync(s => s.AuctionItemId == id && s.UserId == currentUserId);

                hasBid = await _db.Bids
                    .AnyAsync(b => b.AuctionItemId == id && b.UserId == currentUserId);
            }

            return new AuctionItemDetailDTO
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ReservePrice = item.ReservePrice,
                Location = item.Location,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AuctionStartDate = item.AuctionStartDate,
                AuctionEndDate = item.AuctionEndDate,
                Status = AuctionStatusResolver.Resolve(item.Status, item.AuctionStartDate, item.AuctionEndDate),
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
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
                HasCurrentUserBid = hasBid
            };
        }
    }
}