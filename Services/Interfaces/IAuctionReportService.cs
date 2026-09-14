using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAuctionReportService
    {
        Task<AuctionReportViewModel> GetAuctionReportAsync(AuctionReportFilterDTO filter);
        Task<byte[]> GenerateCsvReportAsync(AuctionReportFilterDTO filter);
    }
}
