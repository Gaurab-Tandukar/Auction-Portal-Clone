using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAuctionFilterService
    {
        /// <summary>
        /// Applies all shared AuctionItemFilterDTO filters (category, location,
        /// price range, date range, and master search term) to the given query.
        /// Does NOT apply pagination or any caller-specific rules
        /// (e.g. excluding Draft items) — those stay with the calling service.
        /// </summary>
        IQueryable<AuctionItem> ApplyFilters(IQueryable<AuctionItem> query, AuctionItemFilterDTO filter);
    }
}