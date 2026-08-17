using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auction_Portal_Clone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : Controller // Changed from ControllerBase to Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. GET: /register (Renders the Razor View)
        [HttpGet("/register")]
        public IActionResult Index()
        {
            return View(); // Looks for Views/Register/Index.cshtml
        }

        // 2. POST: api/register (API endpoint for AJAX submission)
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "User with this email already exists." });

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                NationalIdNumber = dto.NationalIdNumber,
                RegisteredAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Assign security role
            string roleToAssign = dto.IsAdminRegistration ? "BankAdmin" : "PublicUser";

            if (!await _roleManager.RoleExistsAsync(roleToAssign))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleToAssign));
            }

            await _userManager.AddToRoleAsync(user, roleToAssign);

            return Ok(new { message = "Registration successful.", userId = user.Id });
        }
    }
}