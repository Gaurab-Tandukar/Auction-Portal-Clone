using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardSummaryDTO> GetSummaryAsync();
    }
}