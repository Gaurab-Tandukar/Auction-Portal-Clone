using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;

namespace Auction_Portal_Clone.Services.Implementation
{
    /// <summary>
    /// Handles saving item attachments (images + PDF documents) to disk
    /// and recording them via IAdminAuctionItemService.
    /// </summary>
    public class AttachmentUploadService : IAttachmentUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IAdminAuctionItemService _adminService;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedDocExtensions = { ".pdf" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public AttachmentUploadService(IWebHostEnvironment env, IAdminAuctionItemService adminService)
        {
            _env = env;
            _adminService = adminService;
        }

        public async Task<ServiceResult<bool>> UploadAttachmentsAsync(
            int auctionItemId,
            List<IFormFile>? images,
            List<IFormFile>? documents)
        {
            var errors = new List<string>();

            if (images is not null)
            {
                foreach (var file in images)
                {
                    var error = await SaveFileAsync(file, auctionItemId, FileType.Image);
                    if (error is not null) errors.Add(error);
                }
            }

            if (documents is not null)
            {
                foreach (var file in documents)
                {
                    var error = await SaveFileAsync(file, auctionItemId, FileType.PDFNotice);
                    if (error is not null) errors.Add(error);
                }
            }

            return errors.Count == 0
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure(string.Join(" ", errors));
        }

        /// <summary>
        /// Saves a single attachment from an arbitrary stream (e.g. a file
        /// extracted from a ZIP during bulk import) using the same storage
        /// rules as the IFormFile upload path:
        /// wwwroot/uploads/{auctionItemId}/{guid}{ext}.
        /// </summary>
        public async Task<ServiceResult<string>> SaveStreamAsync(
            int auctionItemId,
            Stream content,
            long contentLength,
            string originalFileName,
            FileType fileType)
        {
            var allowedExtensions = fileType == FileType.Image ? AllowedImageExtensions : AllowedDocExtensions;

            if (contentLength <= 0)
                return ServiceResult<string>.Failure($"'{originalFileName}' is empty and was skipped.");

            if (contentLength > MaxFileSizeBytes)
                return ServiceResult<string>.Failure($"'{originalFileName}' exceeds the 10 MB limit and was skipped.");

            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return ServiceResult<string>.Failure($"'{originalFileName}' has an unsupported file type and was skipped.");

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", auctionItemId.ToString());
            Directory.CreateDirectory(uploadRoot);

            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadRoot, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/{auctionItemId}/{safeFileName}";
            var addResult = await _adminService.AddAttachmentAsync(auctionItemId, relativeUrl, originalFileName, fileType);
            if (!addResult.Succeeded)
            {
                // Do not leave an orphan file behind when the DB insert fails.
                try { System.IO.File.Delete(fullPath); }
                catch (Exception) { /* best effort cleanup */ }

                return ServiceResult<string>.Failure(
                    $"'{originalFileName}' could not be recorded: {addResult.ErrorMessage}");
            }

            return ServiceResult<string>.Success(relativeUrl);
        }

        private async Task<string?> SaveFileAsync(
            IFormFile file,
            int auctionItemId,
            FileType fileType)
        {
            if (file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();
            var result = await SaveStreamAsync(auctionItemId, stream, file.Length, file.FileName, fileType);
            return result.Succeeded ? null : result.ErrorMessage;
        }
    }
}