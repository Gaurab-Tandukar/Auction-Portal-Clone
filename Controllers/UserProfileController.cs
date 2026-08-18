using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction_Portal_Clone.Controllers
{
    [ApiController]
    [Route("api/user/profile")]
    public class UserProfileController : Controller 
    {
        private readonly UserManager<User> _userManager;

        public UserProfileController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // 1. GET: /profile (Renders the Profile Razor View)
        [HttpGet("/profile")]
        public IActionResult Index()
        {
            return View("~/Views/UserProfile/Index.cshtml");
        }

        // 2. GET: api/user/profile/me (API endpoint for AJAX data fetching)
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User profile not found." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                IsVerifiedForBidding = user.IsVerifiedForBidding,
                Roles = roles
            });
        }
    }
}