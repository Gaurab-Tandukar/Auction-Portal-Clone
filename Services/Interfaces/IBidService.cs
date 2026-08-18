using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IBidService
    {
        Task<ServiceResult<BidDTO>> PlaceBidAsync(string userId, PlaceBidDTO dto);
    }
}