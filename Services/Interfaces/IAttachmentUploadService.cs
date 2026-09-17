namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAttachmentUploadService
    {
        Task<ServiceResult<bool>> UploadAttachmentsAsync(
            int auctionItemId,
            List<IFormFile>? images,
            List<IFormFile>? documents);

        /// <summary>
        /// Saves a single attachment from an arbitrary stream (e.g. a file
        /// extracted from a ZIP during bulk import) using the same storage
        /// rules as the IFormFile upload path. Returns the relative URL of
        /// the saved file.
        /// </summary>
        Task<ServiceResult<string>> SaveStreamAsync(
            int auctionItemId,
            Stream content,
            long contentLength,
            string originalFileName,
            Models.FileType fileType);
    }
}