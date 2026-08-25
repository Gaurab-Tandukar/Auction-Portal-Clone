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
            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", auctionItemId.ToString());
            Directory.CreateDirectory(uploadRoot);

            var errors = new List<string>();

            if (images is not null)
            {
                foreach (var file in images)
                {
                    var error = await SaveFileAsync(file, uploadRoot, auctionItemId, AllowedImageExtensions, FileType.Image);
                    if (error is not null) errors.Add(error);
                }
            }

            if (documents is not null)
            {
                foreach (var file in documents)
                {
                    var error = await SaveFileAsync(file, uploadRoot, auctionItemId, AllowedDocExtensions, FileType.PDFNotice);
                    if (error is not null) errors.Add(error);
                }
            }

            return errors.Count == 0
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure(string.Join(" ", errors));
        }

        private async Task<string?> SaveFileAsync(
            IFormFile file,
            string uploadRoot,
            int auctionItemId,
            string[] allowedExtensions,
            FileType fileType)
        {
            if (file.Length == 0)
                return null;

            if (file.Length > MaxFileSizeBytes)
                return $"'{file.FileName}' exceeds the 10 MB limit and was skipped.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return $"'{file.FileName}' has an unsupported file type and was skipped.";

            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadRoot, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/{auctionItemId}/{safeFileName}";
            await _adminService.AddAttachmentAsync(auctionItemId, relativeUrl, file.FileName, fileType);

            return null;
        }
    }
}