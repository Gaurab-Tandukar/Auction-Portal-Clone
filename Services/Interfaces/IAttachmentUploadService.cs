namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAttachmentUploadService
    {
        Task<ServiceResult<bool>> UploadAttachmentsAsync(
            int auctionItemId,
            List<IFormFile>? images,
            List<IFormFile>? documents);
    }
}