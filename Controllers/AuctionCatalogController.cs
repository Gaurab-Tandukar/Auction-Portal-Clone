using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.Controllers
{
    public class AuctionCatalogController : Controller
    {
        private readonly IAuctionCatalogService _catalogService;
        private readonly UserManager<User> _userManager;

        public AuctionCatalogController(IAuctionCatalogService catalogService, UserManager<User> userManager)
        {
            _catalogService = catalogService;
            _userManager = userManager;
        }

        // GET /AuctionCatalog?categoryId=1&city=Kathmandu&minPrice=1000&page=1
        public async Task<IActionResult> Index([FromQuery] AuctionItemFilterDTO filter)
        {
            var result = await _catalogService.GetCatalogAsync(filter);
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