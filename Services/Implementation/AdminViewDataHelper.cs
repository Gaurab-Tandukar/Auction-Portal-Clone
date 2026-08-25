using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    /// <summary>
    /// Populates dropdown/reference data (categories, locations, attachments)
    /// used by the admin auction item create/edit views.
    /// </summary>
    public class AdminViewDataHelper : IAdminViewDataHelper
    {
        private readonly AuctionDbContext _context;

        public AdminViewDataHelper(AuctionDbContext context)
        {
            _context = context;
        }

        public async Task PopulateCategoriesAsync(ViewDataDictionary viewData, bool activeOnly = true)
        {
            var query = _context.Categories.AsQueryable();
            if (activeOnly)
                query = query.Where(c => c.Active == true);

            viewData["Categories"] = await query.ToListAsync();
        }

        public async Task PopulateLocationViewDataAsync(ViewDataDictionary viewData)
        {
            viewData["Provinces"] = await _context.Provinces
                .OrderBy(p => p.Name)
                .ToListAsync();

            viewData["Districts"] = await _context.Districts
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name, d.ProvinceId })
                .ToListAsync();

            viewData["Municipalities"] = await _context.Municipalities
                .OrderBy(m => m.Name)
                .Select(m => new { m.Id, m.Name, m.DistrictId })
                .ToListAsync();
        }

        public async Task<List<ItemAttachmentDTO>> LoadAttachmentsAsync(int auctionItemId)
        {
            return await _context.ItemAttachments
                .Where(a => a.AuctionItemId == auctionItemId)
                .Select(a => new ItemAttachmentDTO
                {
                    Id = a.Id,
                    FileUrl = a.FileUrl,
                    FileName = a.FileName,
                    FileType = a.FileType
                })
                .ToListAsync();
        }
    }
}