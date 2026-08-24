using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction_Portal_Clone.Controllers.Admin
{
    [Authorize(Roles = "BankStaff")]
    [Route("Admin/Dashboard")]
    public class AdminDashboardController : Controller
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var summary = await _dashboardService.GetSummaryAsync();
            return View(summary);
        }
    }
}