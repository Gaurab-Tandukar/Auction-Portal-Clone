using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Auction_Portal_Clone.Controllers
{
    [ApiController]
    [Route("api/user/profile")]
    public class UserProfileController : Controller
    {
        // Distinguishes this token's purpose from other Identity token uses
        // (e.g. password reset) so a token generated for one can't be
        // replayed for the other.
        private const string BiddingVerificationPurpose = "VerifyBidding";

        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public UserProfileController(UserManager<User> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: /profile (Renders the Profile Razor View)
        [HttpGet("/profile")]
        public IActionResult Index([FromQuery] string? verified)
        {
            ViewData["VerifiedResult"] = verified;
            return View("~/Views/UserProfile/Index.cshtml");
        }

        // GET: api/user/profile/me
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

        // POST: api/user/profile/send-verification
        // Emails a signed verification link to the user's OWN address on
        // file. We never accept an email address as input here — only the
        // logged-in user's own record is used as the destination.
        [HttpPost("send-verification")]
        [Authorize]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User profile not found." });

            if (user.IsVerifiedForBidding)
                return BadRequest(new { message = "This account is already verified for bidding." });

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = "No email address on file for this account." });

            var token = await _userManager.GenerateUserTokenAsync(
                user, TokenOptions.DefaultProvider, BiddingVerificationPurpose);
            var encodedToken = UrlEncoder.Default.Encode(token);

            var confirmUrl = Url.Action(
                action: nameof(ConfirmBiddingVerification),
                controller: "UserProfile",
                values: new { userId = user.Id, token = encodedToken },
                protocol: Request.Scheme,
                host: Request.Host.ToString());

            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                    <h2 style='color:#212529;'>Verify your account for bidding</h2>
                    <p>Hi {WebUtility.HtmlEncode(user.FullName)},</p>
                    <p>Click the button below to verify your account so you can place bids on Siddhartha Bank Auction Portal.</p>
                    <p style='margin: 24px 0;'>
                        <a href='{confirmUrl}' style='background:#FF6B57; color:#fff; padding:12px 24px; border-radius:24px; text-decoration:none; font-weight:bold;'>Verify for Bidding</a>
                    </p>
                    <p style='color:#6c757d; font-size: 13px;'>If the button doesn't work, copy and paste this link into your browser:<br/>{confirmUrl}</p>
                    <p style='color:#6c757d; font-size: 13px;'>This link expires in 24 hours. If you didn't request this, you can safely ignore this email.</p>
                </div>";

            await _emailSender.SendEmailAsync(user.Email, "Verify your account for bidding", htmlMessage);

            return Ok(new { message = "Verification email sent. Please check your inbox." });
        }

        // GET: /profile/confirm-bidding?userId=...&token=...
        // Public link clicked from the emailed verification button.
        // Deliberately not [Authorize] — the token itself is the proof of
        // identity, since the person may open the link in a different
        // browser/session than the one they requested it from.
        [HttpGet("/profile/confirm-bidding")]
        public async Task<IActionResult> ConfirmBiddingVerification(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Index), new { verified = "invalid" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction(nameof(Index), new { verified = "invalid" });

            if (user.IsVerifiedForBidding)
                return RedirectToAction(nameof(Index), new { verified = "already" });

            var isValid = await _userManager.VerifyUserTokenAsync(
                user, TokenOptions.DefaultProvider, BiddingVerificationPurpose, token);

            if (!isValid)
                return RedirectToAction(nameof(Index), new { verified = "invalid" });

            user.IsVerifiedForBidding = true;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index), new { verified = "success" });
        }
    }
}