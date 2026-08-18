using Auction_Portal_Clone.Models;

namespace Auction_Portal_Clone.Services
{
    public static class AuctionStatusResolver
    {
        /// <summary>
        /// Computes the effective display status for an auction item.
        /// Draft and Closed are authoritative (admin-controlled).
        /// Upcoming/Active are derived from the current date vs. the auction window.
        /// </summary>
        public static AuctionStatus Resolve(AuctionStatus storedStatus, DateTime startDate, DateTime endDate, DateTime? now = null)
        {
            // Draft and Closed are set explicitly by admin and always win.
            if (storedStatus == AuctionStatus.Draft || storedStatus == AuctionStatus.Closed)
                return storedStatus;

            var currentTime = now ?? DateTime.UtcNow;

            if (currentTime < startDate)
                return AuctionStatus.Upcoming;

            if (currentTime > endDate)
                return AuctionStatus.Closed;

            return AuctionStatus.Active;
        }
    }
}