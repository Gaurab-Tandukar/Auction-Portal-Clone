using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.Controllers
{
    [Authorize]
    public class SavedListingController : Controller
    {
        private readonly ISavedListingService _savedListingService;
        private readonly UserManager<User> _userManager;

        public SavedListingController(ISavedListingService savedListingService, UserManager<User> userManager)
        {
            _savedListingService = savedListingService;
            _userManager = userManager;
        }

        // GET /SavedListing
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var listings = await _savedListingService.GetUserSavedListingsAsync(userId);
            return View(listings);
        }

        // POST /SavedListing/Toggle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int auctionItemId)
        {
            var userId = _userManager.GetUserId(User)!;
            var result = await _savedListingService.ToggleSaveAsync(userId, auctionItemId);

            if (!result.Succeeded)
                return BadRequest(result.ErrorMessage);

            // result.Data is true = now saved, false = now unsaved
            return Json(new { saved = result.Data });
        }

        // GET /SavedListing/GetWishlistPartial
        // Returns the user's saved listings as JSON for the sidebar overlay.
        [HttpGet]
        public async Task<IActionResult> GetWishlistPartial()
        {
            var userId = _userManager.GetUserId(User)!;
            var listings = await _savedListingService.GetUserSavedListingsAsync(userId);
            return Json(listings);
        }

        // GET /SavedListing/GetWishlistCount
        // Returns the count of saved listings as JSON for the navbar badge.
        [HttpGet]
        public async Task<IActionResult> GetWishlistCount()
        {
            var userId = _userManager.GetUserId(User)!;
            var listings = await _savedListingService.GetUserSavedListingsAsync(userId);
            return Json(new { count = listings.Count });
        }
    }
}