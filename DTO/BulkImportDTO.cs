namespace Auction_Portal_Clone.DTO
{
    /// <summary>
    /// Outcome of a single spreadsheet row during a bulk import.
    /// </summary>
    public class BulkImportRowResultDTO
    {
        public int RowNumber { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool Succeeded { get; set; }

        public int? CreatedItemId { get; set; }

        /// <summary>Clear error message on failure, or a success summary (with optional warnings).</summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Full report of one bulk import run: how many rows were created and
    /// which rows failed (with row number + reason).
    /// </summary>
    public class BulkImportResultDTO
    {
        public int TotalRows { get; set; }

        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        /// <summary>Set when the import failed at file level (bad file type, unreadable workbook, etc.).</summary>
        public string? FileError { get; set; }

        public List<BulkImportRowResultDTO> Rows { get; set; } = new();
    }
}