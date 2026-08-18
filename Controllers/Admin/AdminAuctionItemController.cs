using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction_Portal_Clone.Controllers.Admin
{
    [Authorize(Roles = "BankStaff")]
    [Route("Admin/AuctionItem")]
    public class AdminAuctionItemController : Controller
    {
        private readonly IAdminAuctionItemService _adminService;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedDocExtensions = { ".pdf" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public AdminAuctionItemController(IAdminAuctionItemService adminService, IWebHostEnvironment env)
        {
            _adminService = adminService;
            _env = env;
        }

        // GET /Admin/AuctionItem
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] AuctionItemFilterDTO filter)
        {
            var result = await _adminService.GetAllForAdminAsync(filter);
            return View(result);
        }

        // GET /Admin/AuctionItem/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new AdminAuctionItemCreateDTO());
        }

        // POST /Admin/AuctionItem/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminAuctionItemCreateDTO dto, List<IFormFile>? images, List<IFormFile>? documents)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminService.CreateAsync(dto);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return View(dto);
            }

            int newItemId = result.Data;

            var uploadResult = await UploadAttachmentsAsync(newItemId, images, documents);
            if (!uploadResult.Succeeded)
            {
                TempData["UploadWarning"] = uploadResult.ErrorMessage;
            }

            TempData["Success"] = "Auction item created successfully.";
            return RedirectToAction(nameof(Edit), new { id = newItemId });
        }

        // GET /Admin/AuctionItem/Edit/5
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _adminService.GetByIdForAdminAsync(id);
            if (item is null)
                return NotFound();

            return View(item);
        }

        // POST /Admin/AuctionItem/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminAuctionItemUpdateDTO dto, List<IFormFile>? images, List<IFormFile>? documents)
        {
            if (id != dto.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _adminService.UpdateAsync(dto);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return View(dto);
            }

            var uploadResult = await UploadAttachmentsAsync(id, images, documents);
            if (!uploadResult.Succeeded)
            {
                TempData["UploadWarning"] = uploadResult.ErrorMessage;
            }

            TempData["Success"] = "Auction item updated successfully.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // POST /Admin/AuctionItem/Delete/5
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _adminService.DeleteAsync(id);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData["Success"] = "Auction item deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Admin/AuctionItem/RemoveAttachment/5
        [HttpPost("RemoveAttachment/{attachmentId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAttachment(int attachmentId, int auctionItemId)
        {
            await _adminService.RemoveAttachmentAsync(attachmentId);
            return RedirectToAction(nameof(Edit), new { id = auctionItemId });
        }

        // --- File upload helper ---
        private async Task<ServiceResult<bool>> UploadAttachmentsAsync(int auctionItemId, List<IFormFile>? images, List<IFormFile>? documents)
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

        private async Task<string?> SaveFileAsync(IFormFile file, string uploadRoot, int auctionItemId, string[] allowedExtensions, FileType fileType)
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