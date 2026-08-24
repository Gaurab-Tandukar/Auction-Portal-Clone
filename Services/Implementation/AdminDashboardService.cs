using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Auction_Portal_Clone.Data;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly AuctionDbContext _context; // swap in your actual DbContext name

        public AdminDashboardService(AuctionDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardSummaryDTO> GetSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekAgo = now.AddDays(-7);
            var soonCutoff = now.AddHours(48);

            var summary = new AdminDashboardSummaryDTO();

            // --- Status counts (single grouped query) ---
            var statusCounts = await _context.AuctionItems
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            summary.DraftCount = statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Draft)?.Count ?? 0;
            summary.UpcomingCount = statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Upcoming)?.Count ?? 0;
            summary.ActiveCount = statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Active)?.Count ?? 0;
            summary.ClosedCount = statusCounts.FirstOrDefault(s => s.Status == AuctionStatus.Closed)?.Count ?? 0;

            // --- Health flags ---
            summary.ItemsEndingSoon = await _context.AuctionItems
                .CountAsync(a => a.Status == AuctionStatus.Active && a.AuctionEndDate <= soonCutoff && a.AuctionEndDate > now);

            summary.ItemsPastEndButActive = await _context.AuctionItems
                .CountAsync(a => a.Status == AuctionStatus.Active && a.AuctionEndDate < now);

            summary.ItemsMissingImages = await _context.AuctionItems
                .CountAsync(a => !a.Attachments.Any(att => att.FileType == FileType.Image));

            summary.ItemsMissingPdfNotice = await _context.AuctionItems
                .CountAsync(a => !a.Attachments.Any(att => att.FileType == FileType.PDFNotice));

            summary.ItemsBelowReserve = await _context.AuctionItems
                .Where(a => a.Status == AuctionStatus.Closed)
                .Where(a => !a.Bids.Any() || a.Bids.Max(b => b.OfferedAmount) < a.ReservePrice)
                .CountAsync();

            // --- Bid activity ---
            var todaysBids = await _context.Bids
                .Where(b => b.SubmittedAt >= todayStart)
                .ToListAsync();

            summary.TotalBidsToday = todaysBids.Count;
            summary.TotalBidValueToday = todaysBids.Sum(b => b.OfferedAmount);

            summary.RecentBids = await _context.Bids
                .OrderByDescending(b => b.SubmittedAt)
                .Take(10)
                .Select(b => new RecentBidDTO
                {
                    AuctionItemId = b.AuctionItemId,
                    AuctionItemTitle = b.AuctionItem.Title,
                    UserFullName = b.User.FullName,
                    OfferedAmount = b.OfferedAmount,
                    SubmittedAt = b.SubmittedAt
                })
                .ToListAsync();

            // --- User stats ---
            summary.TotalUsers = await _context.Users.CountAsync();
            summary.NewUsersThisWeek = await _context.Users.CountAsync(u => u.RegisteredAt >= weekAgo);
            summary.VerifiedBidders = await _context.Users.CountAsync(u => u.IsVerifiedForBidding);

            // --- Recently created items ---
            summary.RecentlyCreatedItems = await _context.AuctionItems
                .OrderByDescending(a => a.Id)
                .Take(5)
                .Select(a => new RecentAuctionItemDTO
                {
                    Id = a.Id,
                    Title = a.Title,
                    Status = a.Status,
                    AuctionEndDate = a.AuctionEndDate
                })
                .ToListAsync();

            // --- Items by category ---
            summary.ItemsByCategory = await _context.AuctionItems
                .GroupBy(a => a.Category.Name)
                .Select(g => new CategoryCountDTO { CategoryName = g.Key, ItemCount = g.Count() })
                .OrderByDescending(c => c.ItemCount)
                .ToListAsync();

            return summary;
        }
    }
}