using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAuctionCatalogService
    {
        Task<PagedResultDTO<AuctionItemListItemDTO>> GetCatalogAsync(AuctionItemFilterDTO filter);

        Task<AuctionItemDetailDTO?> GetDetailAsync(int id, string? currentUserId);
    }
}