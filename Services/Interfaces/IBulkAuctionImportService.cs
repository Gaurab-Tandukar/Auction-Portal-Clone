using Auction_Portal_Clone.DTO;

namespace Auction_Portal_Clone.Services.Interfaces
{
    public interface IBulkAuctionImportService
    {
        /// <summary>
        /// Imports auction items from an .xlsx spreadsheet with an optional
        /// ZIP of images and PDF notices. Returns a per-row report of
        /// successes and failures.
        /// </summary>
        Task<BulkImportResultDTO> ImportAsync(IFormFile spreadsheet, IFormFile? zipFile);

        /// <summary>Builds a downloadable .xlsx template with the correct headers and example rows.</summary>
        byte[] GenerateTemplate();
    }
}