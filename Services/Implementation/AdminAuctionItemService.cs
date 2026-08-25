using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class AdminAuctionItemService : IAdminAuctionItemService
    {
        private readonly AuctionDbContext _db;
        private readonly IAuctionFilterService _filterService;

        public AdminAuctionItemService(AuctionDbContext db, IAuctionFilterService filterService)
        {
            _db = db;
            _filterService = filterService;
        }


        public async Task<PagedResultDTO<AuctionItemListItemDTO>> GetAllForAdminAsync(AuctionItemFilterDTO filter)
        {
            var query = _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Attachments)
                .AsQueryable();

            query = _filterService.ApplyFilters(query, filter);

            var totalCount = await query.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 12 : filter.PageSize;

            var items = await query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuctionItemListItemDTO
                {
                    Id = a.Id,
                    Title = a.Title,
                    MunicipalityName = a.Municipality.Name,
                    DistrictName = a.Municipality.District.Name,
                    ReservePrice = a.ReservePrice,
                    AuctionStartDate = a.AuctionStartDate,
                    AuctionEndDate = a.AuctionEndDate,
                    Status = a.Status,
                    CategoryName = a.Category.Name,
                    ThumbnailUrl = a.Attachments
                        .Where(att => att.FileType == FileType.Image)
                        .Select(att => att.FileUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResultDTO<AuctionItemListItemDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AuctionItemDetailDTO?> GetByIdForAdminAsync(int id)
        {
            var item = await _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Attachments)
                .Include(a => a.Municipality).ThenInclude(m => m.District).ThenInclude(d => d.Province)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (item is null)
                return null;

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
                Status = item.Status,
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
                    .ToList()
            };
        }
        public async Task<ServiceResult<int>> CreateAsync(AdminAuctionItemCreateDTO dto)
        {
            if (dto.AuctionEndDate <= dto.AuctionStartDate)
                return ServiceResult<int>.Failure("Auction end date must be after the start date.");

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return ServiceResult<int>.Failure("Selected category does not exist.");

            var item = new AuctionItem
            {
                Title = dto.Title,
                Description = dto.Description,
                ReservePrice = dto.ReservePrice,
                MunicipalityId = dto.MunicipalityId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                AuctionStartDate = dto.AuctionStartDate,
                AuctionEndDate = dto.AuctionEndDate,
                Status = dto.Status,
                CategoryId = dto.CategoryId
            };

            _db.AuctionItems.Add(item);
            await _db.SaveChangesAsync();

            return ServiceResult<int>.Success(item.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(AdminAuctionItemUpdateDTO dto)
        {
            var item = await _db.AuctionItems.FirstOrDefaultAsync(a => a.Id == dto.Id);
            if (item is null)
                return ServiceResult<bool>.Failure("Auction item not found.");

            if (dto.AuctionEndDate <= dto.AuctionStartDate)
                return ServiceResult<bool>.Failure("Auction end date must be after the start date.");

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return ServiceResult<bool>.Failure("Selected category does not exist.");

            item.Title = dto.Title;
            item.Description = dto.Description;
            item.ReservePrice = dto.ReservePrice;
            item.MunicipalityId = dto.MunicipalityId;
            item.Latitude = dto.Latitude;
            item.Longitude = dto.Longitude;
            item.AuctionStartDate = dto.AuctionStartDate;
            item.AuctionEndDate = dto.AuctionEndDate;
            item.Status = dto.Status;
            item.CategoryId = dto.CategoryId;

            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var item = await _db.AuctionItems.FirstOrDefaultAsync(a => a.Id == id);
            if (item is null)
                return ServiceResult<bool>.Failure("Auction item not found.");

            var hasBids = await _db.Bids.AnyAsync(b => b.AuctionItemId == id);
            if (hasBids)
                return ServiceResult<bool>.Failure("Cannot delete an item that already has bids. Consider closing it instead.");

            _db.AuctionItems.Remove(item);
            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<int>> AddAttachmentAsync(int auctionItemId, string fileUrl, string? fileName, FileType fileType)
        {
            var itemExists = await _db.AuctionItems.AnyAsync(a => a.Id == auctionItemId);
            if (!itemExists)
                return ServiceResult<int>.Failure("Auction item not found.");

            var attachment = new ItemAttachment
            {
                AuctionItemId = auctionItemId,
                FileUrl = fileUrl,
                FileName = fileName,
                FileType = fileType
            };

            _db.ItemAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            return ServiceResult<int>.Success(attachment.Id);
        }

        public async Task<ServiceResult<bool>> RemoveAttachmentAsync(int attachmentId)
        {
            var attachment = await _db.ItemAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId);
            if (attachment is null)
                return ServiceResult<bool>.Failure("Attachment not found.");

            _db.ItemAttachments.Remove(attachment);
            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }
    }
}