using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auction_Portal_Clone.Controllers
{
    public class AuctionCatalogController : Controller
    {
        private readonly IAuctionCatalogService _catalogService;
        private readonly UserManager<User> _userManager;
        private readonly IAdminViewDataHelper _viewDataHelper;

        public AuctionCatalogController(
            IAuctionCatalogService catalogService,
            UserManager<User> userManager,
            IAdminViewDataHelper viewDataHelper)
        {
            _catalogService = catalogService;
            _userManager = userManager;
            _viewDataHelper = viewDataHelper;
        }

        // GET /AuctionCatalog
        // GET /AuctionCatalog?SearchTerm=land&CategoryId=1&ProvinceId=2&Page=1
        public async Task<IActionResult> Index([FromQuery] AuctionItemFilterDTO filter)
        {
            var result = await _catalogService.GetCatalogAsync(filter);

            // Keep current filter values for the search bar + advanced panel
            ViewData["Filter"] = filter;

            // Fill Category dropdown
            await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: true);

            // Fill Province / District / Municipality dropdowns
            await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);

            return View(result);
        }

        // GET /AuctionCatalog/Collateral/Land
        // GET /AuctionCatalog/Collateral/ResidentialProperty
        // GET /AuctionCatalog/Collateral/Commercial
        // GET /AuctionCatalog/Collateral/Vehicle
        [HttpGet]
        public async Task<IActionResult> Collateral(string id, [FromQuery] AuctionItemFilterDTO filter)
        {
            var collateralCategory = CollateralCategoryExtensions.ParseCollateralCategory(id);
            if (!collateralCategory.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            filter.CollateralCategory = collateralCategory.Value;

            var result = await _catalogService.GetCatalogAsync(filter);

            // Keep current filter values for the search bar + advanced panel
            ViewData["Filter"] = filter;
            ViewData["SelectedCollateralCategory"] = collateralCategory.Value;
            ViewData["SelectedCollateralName"] = collateralCategory.Value.ToDisplayName();

            // Fill Category dropdown
            await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: true);

            // Fill Province / District / Municipality dropdowns
            await _viewDataHelper.PopulateLocationViewDataAsync(ViewData);

            return View(result);
        }

        // GET /AuctionCatalog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            string? userId = _userManager.GetUserId(User);

            var item = await _catalogService.GetDetailAsync(id, userId);
            if (item is null)
                return NotFound();

            return View(item);
        }
    }
}