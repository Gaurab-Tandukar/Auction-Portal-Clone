using System.Globalization;
using System.Text;
using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class AuctionReportService : IAuctionReportService
    {
        private readonly AuctionDbContext _db;

        public AuctionReportService(AuctionDbContext db)
        {
            _db = db;
        }

        public async Task<AuctionReportViewModel> GetAuctionReportAsync(AuctionReportFilterDTO filter)
        {
            var now = DateTime.UtcNow;

            // Base query of ended auctions: either marked closed, past end date, or finalized
            var baseQuery = _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Municipality)
                    .ThenInclude(m => m.District)
                .Include(a => a.WinnerUser)
                .Include(a => a.Bids)
                .Where(a => a.Status != AuctionStatus.Draft && (a.AuctionEndDate <= now || a.Status == AuctionStatus.Closed || a.FinalStatus != AuctionFinalStatus.Pending))
                .AsQueryable();

            // Calculate overall summaries for ended auctions (prior to pagination and specific page filters, but respecting date range if provided)
            var summaryQuery = baseQuery;
            if (filter.DateFrom.HasValue)
                summaryQuery = summaryQuery.Where(a => a.AuctionEndDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                summaryQuery = summaryQuery.Where(a => a.AuctionEndDate <= filter.DateTo.Value.Date.AddDays(1).AddTicks(-1));

            var totalEnded = await summaryQuery.CountAsync();
            var totalSold = await summaryQuery.CountAsync(a => a.FinalStatus == AuctionFinalStatus.Sold);
            var totalUnsold = await summaryQuery.CountAsync(a => a.FinalStatus == AuctionFinalStatus.Unsold);
            var totalPending = await summaryQuery.CountAsync(a => a.FinalStatus == AuctionFinalStatus.Pending);
            var totalRevenue = await summaryQuery.Where(a => a.FinalStatus == AuctionFinalStatus.Sold).SumAsync(a => a.WinningAmount ?? 0m);
            var totalBids = await summaryQuery.SelectMany(a => a.Bids).CountAsync();

            var summary = new AuctionReportSummaryDTO
            {
                TotalEndedAuctions = totalEnded,
                TotalSoldAuctions = totalSold,
                TotalUnsoldAuctions = totalUnsold,
                TotalPendingAuctions = totalPending,
                TotalWinningValue = totalRevenue,
                TotalBidsCount = totalBids
            };

            // Apply specific table filters
            var filteredQuery = baseQuery;

            if (filter.DateFrom.HasValue)
                filteredQuery = filteredQuery.Where(a => a.AuctionEndDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                filteredQuery = filteredQuery.Where(a => a.AuctionEndDate <= filter.DateTo.Value.Date.AddDays(1).AddTicks(-1));

            if (filter.FinalStatus.HasValue)
                filteredQuery = filteredQuery.Where(a => a.FinalStatus == filter.FinalStatus.Value);

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
                filteredQuery = filteredQuery.Where(a => a.CategoryId == filter.CategoryId.Value);

            if (filter.CollateralCategory.HasValue)
                filteredQuery = filteredQuery.Where(a => a.CollateralCategory == filter.CollateralCategory.Value);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var term = filter.Keyword.Trim();
                filteredQuery = filteredQuery.Where(a =>
                    a.Title.Contains(term) ||
                    (a.WinnerUser != null && (a.WinnerUser.FullName.Contains(term) || a.WinnerUser.Email!.Contains(term))));
            }

            var totalCount = await filteredQuery.CountAsync();

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 15 : filter.PageSize;

            var rawItems = await filteredQuery
                .OrderByDescending(a => a.AuctionEndDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    CategoryName = a.Category.Name,
                    a.CollateralCategory,
                    MunicipalityName = a.Municipality.Name,
                    DistrictName = a.Municipality.District.Name,
                    a.ReservePrice,
                    a.AuctionStartDate,
                    a.AuctionEndDate,
                    a.Status,
                    a.FinalStatus,
                    a.WinningAmount,
                    a.WinnerUserId,
                    WinnerName = a.WinnerUser != null ? a.WinnerUser.FullName : null,
                    WinnerEmail = a.WinnerUser != null ? a.WinnerUser.Email : null,
                    WinnerPhone = a.WinnerUser != null ? a.WinnerUser.PhoneNumber : null,
                    a.WinnerDeterminedAt,
                    a.WinnerNotifiedAt,
                    TotalBids = a.Bids.Count,
                    HighestBid = a.Bids.Max(b => (decimal?)b.OfferedAmount)
                })
                .ToListAsync();

            var items = rawItems.Select(a => new AuctionReportItemDTO
            {
                AuctionItemId = a.Id,
                Title = a.Title,
                CategoryName = a.CategoryName,
                CollateralCategory = a.CollateralCategory,
                CollateralCategoryName = a.CollateralCategory.ToDisplayName(),
                MunicipalityName = a.MunicipalityName,
                DistrictName = a.DistrictName,
                ReservePrice = a.ReservePrice,
                AuctionStartDate = a.AuctionStartDate,
                AuctionEndDate = a.AuctionEndDate,
                Status = a.Status,
                FinalStatus = a.FinalStatus,
                WinningAmount = a.WinningAmount,
                WinnerUserId = a.WinnerUserId,
                WinnerName = a.WinnerName,
                WinnerEmail = a.WinnerEmail,
                WinnerPhoneNumber = a.WinnerPhone,
                WinnerDeterminedAt = a.WinnerDeterminedAt,
                WinnerNotifiedAt = a.WinnerNotifiedAt,
                TotalBids = a.TotalBids,
                HighestBidAmount = a.HighestBid
            }).ToList();

            return new AuctionReportViewModel
            {
                Summary = summary,
                PagedItems = new PagedResultDTO<AuctionReportItemDTO>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                },
                Filter = filter
            };
        }

        public async Task<byte[]> GenerateCsvReportAsync(AuctionReportFilterDTO filter)
        {
            var now = DateTime.UtcNow;

            var query = _db.AuctionItems
                .Include(a => a.Category)
                .Include(a => a.Municipality)
                    .ThenInclude(m => m.District)
                .Include(a => a.WinnerUser)
                .Include(a => a.Bids)
                .Where(a => a.Status != AuctionStatus.Draft && (a.AuctionEndDate <= now || a.Status == AuctionStatus.Closed || a.FinalStatus != AuctionFinalStatus.Pending))
                .AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(a => a.AuctionEndDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(a => a.AuctionEndDate <= filter.DateTo.Value.Date.AddDays(1).AddTicks(-1));

            if (filter.FinalStatus.HasValue)
                query = query.Where(a => a.FinalStatus == filter.FinalStatus.Value);

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
                query = query.Where(a => a.CategoryId == filter.CategoryId.Value);

            if (filter.CollateralCategory.HasValue)
                query = query.Where(a => a.CollateralCategory == filter.CollateralCategory.Value);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var term = filter.Keyword.Trim();
                query = query.Where(a =>
                    a.Title.Contains(term) ||
                    (a.WinnerUser != null && (a.WinnerUser.FullName.Contains(term) || a.WinnerUser.Email!.Contains(term))));
            }

            var items = await query
                .OrderByDescending(a => a.AuctionEndDate)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    Category = a.Category.Name,
                    Collateral = a.CollateralCategory.ToString(),
                    Municipality = a.Municipality.Name,
                    District = a.Municipality.District.Name,
                    a.ReservePrice,
                    a.AuctionStartDate,
                    a.AuctionEndDate,
                    FinalStatus = a.FinalStatus.ToString(),
                    a.WinningAmount,
                    WinnerName = a.WinnerUser != null ? a.WinnerUser.FullName : "N/A",
                    WinnerEmail = a.WinnerUser != null ? a.WinnerUser.Email : "N/A",
                    WinnerPhone = a.WinnerUser != null ? a.WinnerUser.PhoneNumber : "N/A",
                    TotalBids = a.Bids.Count,
                    HighestBid = a.Bids.Max(b => (decimal?)b.OfferedAmount),
                    a.WinnerDeterminedAt
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Auction ID,Title,Category,Collateral,Location,Reserve Price (NPR),Auction End Date,Final Status,Winning Amount (NPR),Winner Name,Winner Email,Winner Phone,Total Bids,Highest Bid (NPR),Determined At");

            foreach (var item in items)
            {
                sb.AppendLine(string.Join(",",
                    item.Id,
                    EscapeCsv(item.Title),
                    EscapeCsv(item.Category),
                    EscapeCsv(item.Collateral),
                    EscapeCsv($"{item.Municipality}, {item.District}"),
                    item.ReservePrice.ToString("F2", CultureInfo.InvariantCulture),
                    item.AuctionEndDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    item.FinalStatus,
                    item.WinningAmount.HasValue ? item.WinningAmount.Value.ToString("F2", CultureInfo.InvariantCulture) : "",
                    EscapeCsv(item.WinnerName ?? "N/A"),
                    EscapeCsv(item.WinnerEmail ?? "N/A"),
                    EscapeCsv(item.WinnerPhone ?? "N/A"),
                    item.TotalBids,
                    item.HighestBid.HasValue ? item.HighestBid.Value.ToString("F2", CultureInfo.InvariantCulture) : "",
                    item.WinnerDeterminedAt.HasValue ? item.WinnerDeterminedAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : ""
                ));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return $"\"{field}\"";
        }
    }
}
