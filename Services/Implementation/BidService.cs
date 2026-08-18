using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    public class BidService : IBidService
    {
        private readonly AuctionDbContext _db;

        public BidService(AuctionDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<BidDTO>> PlaceBidAsync(string userId, PlaceBidDTO dto)
        {
            var item = await _db.AuctionItems.FirstOrDefaultAsync(a => a.Id == dto.AuctionItemId);

            if (item is null)
                return ServiceResult<BidDTO>.Failure("Auction item not found.");

            if (item.Status != AuctionStatus.Active)
                return ServiceResult<BidDTO>.Failure("Bidding is not open for this item.");

            var now = DateTime.UtcNow;
            if (now < item.AuctionStartDate || now > item.AuctionEndDate)
                return ServiceResult<BidDTO>.Failure("Auction is not currently within its bidding window.");

            if (dto.OfferedAmount < item.ReservePrice)
                return ServiceResult<BidDTO>.Failure($"Bid must be at least the reserve price of {item.ReservePrice:C}.");

            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return ServiceResult<BidDTO>.Failure("User not found.");

            if (!user.IsVerifiedForBidding)
                return ServiceResult<BidDTO>.Failure("Your account is not yet verified for bidding.");

            var bid = new Bid
            {
                UserId = userId,
                AuctionItemId = dto.AuctionItemId,
                OfferedAmount = dto.OfferedAmount,
                SubmittedAt = now
            };

            _db.Bids.Add(bid);
            await _db.SaveChangesAsync();

            return ServiceResult<BidDTO>.Success(new BidDTO
            {
                Id = bid.Id,
                UserId = userId,
                UserDisplayName = user.FullName,
                OfferedAmount = bid.OfferedAmount,
                SubmittedAt = bid.SubmittedAt
            });
        }
    }
}