using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAuctionWinnerService
    {
        /// <summary>
        /// Scans all auctions whose EndDate has passed, have not yet had a winner determined,
        /// and are not in Draft status. Determines winner or marks unsold, updates status, and notifies winners.
        /// Idempotent and thread-safe.
        /// </summary>
        Task<ServiceResult<AuctionWinnerProcessResultDTO>> ProcessEndedAuctionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines winner for a single specified auction item.
        /// </summary>
        Task<ServiceResult<AuctionWinnerResultDTO>> DetermineWinnerForAuctionAsync(int auctionItemId, CancellationToken cancellationToken = default);
    }
}
