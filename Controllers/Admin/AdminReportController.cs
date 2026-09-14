using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction_Portal_Clone.Controllers.Admin
{
    [Authorize(Roles = "BankStaff")]
    [Route("Admin/Reports")]
    public class AdminReportController : Controller
    {
        private readonly IAuctionReportService _reportService;
        private readonly IAuctionWinnerService _winnerService;
        private readonly IAdminViewDataHelper _viewDataHelper;

        public AdminReportController(
            IAuctionReportService reportService,
            IAuctionWinnerService winnerService,
            IAdminViewDataHelper viewDataHelper)
        {
            _reportService = reportService;
            _winnerService = winnerService;
            _viewDataHelper = viewDataHelper;
        }

        // GET: /Admin/Reports
        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] AuctionReportFilterDTO filter)
        {
            var report = await _reportService.GetAuctionReportAsync(filter);

            await _viewDataHelper.PopulateCategoriesAsync(ViewData, activeOnly: false);

            return View(report);
        }

        // POST: /Admin/Reports/FinalizeEnded
        [HttpPost("FinalizeEnded")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeEnded([FromQuery] string? returnUrl)
        {
            var result = await _winnerService.ProcessEndedAuctionsAsync();

            if (result.Succeeded && result.Data != null)
            {
                var summary = result.Data;
                if (summary.TotalChecked == 0)
                {
                    TempData["InfoMessage"] = "No pending ended auctions were found to finalize.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Finalization complete! {summary.TotalChecked} auctions checked: {summary.TotalSold} marked Sold, {summary.TotalUnsold} marked Unsold, and {summary.TotalEmailsSent} winner emails dispatched.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "An error occurred while finalizing auctions.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Reports/ExportCsv
        [HttpGet("ExportCsv")]
        public async Task<IActionResult> ExportCsv([FromQuery] AuctionReportFilterDTO filter)
        {
            var csvData = await _reportService.GenerateCsvReportAsync(filter);
            var fileName = $"SBL_Auction_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvData, "text/csv", fileName);
        }
    }
}
