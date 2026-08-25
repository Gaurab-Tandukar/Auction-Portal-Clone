using Auction_Portal_Clone.DTO;
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
        private readonly IAttachmentUploadService _uploadService;
        private readonly IAdminViewDataHelper _viewDataHelper;

        public AdminAuctionItemController(
            IAdminAuctionItemService adminService,
            IAttachmentUploadService uploadService,
            IAdminViewDataHelper viewDataHelper)
        {
            _adminService = adminService;
            _uploadService = uploadService;
            _viewDataHelper = viewDataHelper;
        }

        // GET /Admin/AuctionItem
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] AuctionItemFilterDTO filter)
        {
            var result = await _adminService.GetAllForAdminAsync(filter);
            ViewData["Filter"] = filter;

            await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: true);
            await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);

            return View(result);
        }

        // GET /Admin/AuctionItem/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: true);
            await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);

            return View(new AdminAuctionItemCreateDTO());
        }

        // POST /Admin/AuctionItem/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminAuctionItemCreateDTO dto, List<IFormFile>? images, List<IFormFile>? documents)
        {
            if (!ModelState.IsValid)
            {
                await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: true);
                await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);
                return View(dto);
            }

            var result = await _adminService.CreateAsync(dto);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: true);
                await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);
                return View(dto);
            }

            int newItemId = result.Data;

            var uploadResult = await _uploadService.UploadAttachmentsAsync(newItemId, images, documents);
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

            // Map AuctionItemDetailDTO -> AdminAuctionItemUpdateDTO so the view's
            // model type matches what the POST action binds to.
            var dto = new AdminAuctionItemUpdateDTO
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ReservePrice = item.ReservePrice,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AuctionStartDate = item.AuctionStartDate,
                AuctionEndDate = item.AuctionEndDate,
                Status = item.Status,
                CategoryId = item.CategoryId,
                MunicipalityId = item.MunicipalityId
            };

            await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: false);
            ViewData["Attachments"] = await _viewDataHelper.LoadAttachmentsAsync(id);
            await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);

            return View(dto);
        }

        // POST /Admin/AuctionItem/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminAuctionItemUpdateDTO dto, List<IFormFile>? images, List<IFormFile>? documents)
        {
            if (id != dto.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: false);
                ViewData["Attachments"] = await _viewDataHelper.LoadAttachmentsAsync(id);
                await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);
                return View(dto);
            }

            var result = await _adminService.UpdateAsync(dto);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: false);
                ViewData["Attachments"] = await _viewDataHelper.LoadAttachmentsAsync(id);
                await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);
                return View(dto);
            }

            var uploadResult = await _uploadService.UploadAttachmentsAsync(id, images, documents);
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
    }
}