using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auction_Portal_Clone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : Controller // Changed from ControllerBase to Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public LoginController(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 1. GET: /login (Renders the Razor View)
        [HttpGet("/login")]
        public IActionResult Index()
        {
            return View(); // Looks for Views/Login/Index.cshtml
        }

        // 2. POST: api/login (API endpoint for AJAX submission)
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, dto.Password, dto.RememberMe, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return BadRequest(new { message = "Account is locked due to multiple failed login attempts." });

            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                NationalIdNumber = user.NationalIdNumber,
                IsVerifiedForBidding = user.IsVerifiedForBidding,
                Roles = roles
            });
        }
    }
}