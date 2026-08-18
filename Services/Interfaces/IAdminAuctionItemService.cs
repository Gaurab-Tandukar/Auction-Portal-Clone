using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAdminAuctionItemService
    {
        Task<PagedResultDTO<AuctionItemListItemDTO>> GetAllForAdminAsync(AuctionItemFilterDTO filter);

        Task<AuctionItemDetailDTO?> GetByIdForAdminAsync(int id);

        Task<ServiceResult<int>> CreateAsync(AdminAuctionItemCreateDTO dto);

        Task<ServiceResult<bool>> UpdateAsync(AdminAuctionItemUpdateDTO dto);

        Task<ServiceResult<bool>> DeleteAsync(int id);

        Task<ServiceResult<int>> AddAttachmentAsync(int auctionItemId, string fileUrl, string? fileName, Models.FileType fileType);

        Task<ServiceResult<bool>> RemoveAttachmentAsync(int attachmentId);
    }
}