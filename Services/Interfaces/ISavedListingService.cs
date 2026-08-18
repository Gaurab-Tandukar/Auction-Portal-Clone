using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface ISavedListingService
    {
        Task<ServiceResult<bool>> ToggleSaveAsync(string userId, int auctionItemId);

        Task<List<SavedListingDTO>> GetUserSavedListingsAsync(string userId);
    }
}