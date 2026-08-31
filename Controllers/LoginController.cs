using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction_Portal_Clone.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public LoginController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginRequestDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed attempts. Try again in 15 minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            }

            return View(model);
        }

        // GET: /Login/GoogleLogin
        // Initiates Google OAuth for direct sign in / seamless registration with bidding verification
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Login", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: /Login/GoogleCallback
        // Handles Google OAuth sign-in, auto-registers if new, sets IsVerifiedForBidding = true, and signs in
        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Google authentication error: {remoteError}";
                return RedirectToAction(nameof(Index));
            }

            var externalAuth = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!externalAuth.Succeeded || externalAuth.Principal == null)
            {
                TempData["ErrorMessage"] = "Failed to authenticate with Google. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            var email = externalAuth.Principal.FindFirstValue(ClaimTypes.Email)
                        ?? externalAuth.Principal.FindFirst("email")?.Value;
            var name = externalAuth.Principal.FindFirstValue(ClaimTypes.Name)
                       ?? externalAuth.Principal.FindFirst("name")?.Value
                       ?? "Google User";

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Google did not provide an email address.";
                return RedirectToAction(nameof(Index));
            }

            // Find existing user or create a new one automatically
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true,
                    IsVerifiedForBidding = true,
                    RegisteredAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                // Ensure verified for bidding and email confirmed since signed in with Google
                if (!user.IsVerifiedForBidding || !user.EmailConfirmed)
                {
                    user.IsVerifiedForBidding = true;
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }
            }

            // Clear temporary external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Sign user in to the application session
            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect(returnUrl);
        }
    }
}