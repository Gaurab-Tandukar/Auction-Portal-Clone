using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.Controllers
{
    [Authorize]
    public class BidController : Controller
    {
        private readonly IBidService _bidService;
        private readonly UserManager<User> _userManager;

        public BidController(IBidService bidService, UserManager<User> userManager)
        {
            _bidService = bidService;
            _userManager = userManager;
        }

        // POST /Bid/Place
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(PlaceBidDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User)!;
            var result = await _bidService.PlaceBidAsync(userId, dto);

            if (!result.Succeeded)
            {
                TempData["BidError"] = result.ErrorMessage;
                return RedirectToAction("Details", "AuctionCatalog", new { id = dto.AuctionItemId });
            }

            TempData["BidSuccess"] = "Your bid has been placed successfully.";
            return RedirectToAction("Details", "AuctionCatalog", new { id = dto.AuctionItemId });
        }
    }
}