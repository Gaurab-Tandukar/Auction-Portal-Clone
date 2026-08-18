using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class SavedListingService : ISavedListingService
    {
        private readonly AuctionDbContext _db;

        public SavedListingService(AuctionDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<bool>> ToggleSaveAsync(string userId, int auctionItemId)
        {
            var itemExists = await _db.AuctionItems.AnyAsync(a => a.Id == auctionItemId);
            if (!itemExists)
                return ServiceResult<bool>.Failure("Auction item not found.");

            var existing = await _db.SavedListings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.AuctionItemId == auctionItemId);

            if (existing is not null)
            {
                _db.SavedListings.Remove(existing);
                await _db.SaveChangesAsync();
                return ServiceResult<bool>.Success(false); // now unsaved
            }

            _db.SavedListings.Add(new SavedListing
            {
                UserId = userId,
                AuctionItemId = auctionItemId,
                SavedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(true); // now saved
        }

        public async Task<List<SavedListingDTO>> GetUserSavedListingsAsync(string userId)
        {
            return await _db.SavedListings
                .Where(s => s.UserId == userId)
                .Include(s => s.AuctionItem)
                    .ThenInclude(a => a.Attachments)
                .OrderByDescending(s => s.SavedAt)
                .Select(s => new SavedListingDTO
                {
                    AuctionItemId = s.AuctionItemId,
                    Title = s.AuctionItem.Title,
                    ThumbnailUrl = s.AuctionItem.Attachments
                        .Where(a => a.FileType == FileType.Image)
                        .Select(a => a.FileUrl)
                        .FirstOrDefault(),
                    ReservePrice = s.AuctionItem.ReservePrice,
                    AuctionEndDate = s.AuctionItem.AuctionEndDate,
                    SavedAt = s.SavedAt
                })
                .ToListAsync();
        }
    }
}