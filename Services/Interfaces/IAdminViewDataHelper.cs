using Auction_Portal_Clone.DTO;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IAdminViewDataHelper
    {
        Task PopulateCategoriesAsync(ViewDataDictionary viewData, bool activeOnly = true);
        Task PopulateLocationViewDataAsync(ViewDataDictionary viewData);
        Task<List<ItemAttachmentDTO>> LoadAttachmentsAsync(int auctionItemId);
    }
}