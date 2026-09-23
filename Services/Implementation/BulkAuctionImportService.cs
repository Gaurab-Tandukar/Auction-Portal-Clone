using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.DTO;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Services.Implementation
{
    /// <summary>
    /// Bulk import of auction items from an Excel (.xlsx) spreadsheet with an
    /// optional ZIP of images and PDF notices. Deliberately reuses
    /// AdminAuctionItemService (CreateAsync / AddAttachmentAsync) and
    /// AttachmentUploadService (SaveStreamAsync) so imported items and their
    /// files are stored exactly like items created through the single-item flow.
    ///
    /// Media matching: the ZIP is expected to contain a parent folder with one
    /// numbered subfolder per row (e.g. "1/", "2/", "3/"...). A row's
    /// MediaFolder column holds that number; every image and PDF found inside
    /// the matching subfolder is attached to the row's item automatically
    /// (images vs. PDF is decided purely by file extension). Any wrapping
    /// top-level folder name in the ZIP is ignored — only the immediate
    /// parent folder of each file needs to be the plain number.
    /// </summary>
    public class BulkAuctionImportService : IBulkAuctionImportService
    {
        /// <summary>Maximum data rows processed per import. Adjust here if a larger batch is ever needed.</summary>
        public const int MaxRowsPerImport = 200;

        private const long MaxSpreadsheetBytes = 10 * 1024 * 1024;       // 10 MB
        private const long MaxZipBytes = 100 * 1024 * 1024;              // 100 MB
        private const long MaxZipUncompressedBytes = 300 * 1024 * 1024;  // zip-bomb guard
        private const long MaxMediaFileBytes = 10 * 1024 * 1024;         // 10 MB per file

        private static readonly string[] AllowedSpreadsheetExtensions = { ".xlsx" };
        private static readonly string[] AllowedZipExtensions = { ".zip" };
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedDocumentExtensions = { ".pdf" };

        // Exact spreadsheet headers (matched case-insensitively).
        internal static readonly string[] Headers =
        {
            "Title", "Description", "ReservePrice", "Latitude", "Longitude",
            "AuctionStartDate", "AuctionEndDate", "Status", "CollateralCategory",
            "CategoryName", "ProvinceName", "DistrictName", "MunicipalityName",
            "MediaFolder"
        };

        private static readonly string[] RequiredHeaders =
        {
            "Title", "ReservePrice", "AuctionStartDate", "AuctionEndDate", "Status",
            "CollateralCategory", "CategoryName", "ProvinceName", "DistrictName", "MunicipalityName"
        };

        // Common date formats accepted in date columns.
        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy",
            "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm", "MM/dd/yyyy",
            "dd-MM-yyyy HH:mm", "dd.MM.yyyy HH:mm",
            "M/d/yyyy h:mm tt", "M/d/yyyy"
        };

        private readonly AuctionDbContext _db;
        private readonly IAdminAuctionItemService _adminService;
        private readonly IAttachmentUploadService _uploadService;
        private readonly ILogger<BulkAuctionImportService> _logger;

        public BulkAuctionImportService(
            AuctionDbContext db,
            IAdminAuctionItemService adminService,
            IAttachmentUploadService uploadService,
            ILogger<BulkAuctionImportService> logger)
        {
            _db = db;
            _adminService = adminService;
            _uploadService = uploadService;
            _logger = logger;
        }

        private sealed record ParsedRow(int RowNumber, Dictionary<string, string> Cells);

        public async Task<BulkImportResultDTO> ImportAsync(IFormFile spreadsheet, IFormFile? zipFile)
        {
            var result = new BulkImportResultDTO();

            // ── 1. Validate the spreadsheet file ─────────────────────────────
            if (spreadsheet is null || spreadsheet.Length == 0)
            {
                result.FileError = "No spreadsheet was uploaded.";
                return result;
            }
            if (!HasExtension(spreadsheet.FileName, AllowedSpreadsheetExtensions))
            {
                result.FileError = "The spreadsheet must be an .xlsx file.";
                return result;
            }
            if (spreadsheet.Length > MaxSpreadsheetBytes)
            {
                result.FileError = "The spreadsheet exceeds the 10 MB limit.";
                return result;
            }

            // ── 2. Load the optional ZIP into memory, grouped by numbered subfolder ──
            Dictionary<string, Dictionary<string, byte[]>>? zipFolders = null;
            var folderWarnings = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            if (zipFile is not null && zipFile.Length > 0)
            {
                if (!HasExtension(zipFile.FileName, AllowedZipExtensions))
                {
                    result.FileError = "The media archive must be a .zip file.";
                    return result;
                }
                if (zipFile.Length > MaxZipBytes)
                {
                    result.FileError = "The ZIP file exceeds the 100 MB limit.";
                    return result;
                }

                zipFolders = await LoadZipAsync(zipFile, folderWarnings, result);
                if (zipFolders is null)
                    return result; // FileError already set inside LoadZipAsync
            }

            // ── 3. Parse the spreadsheet into data rows ──────────────────────
            List<ParsedRow> dataRows;
            try
            {
                dataRows = ParseSpreadsheet(spreadsheet, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk import: the uploaded spreadsheet could not be read.");
                result.FileError = "The spreadsheet could not be read. Make sure it is a valid .xlsx workbook.";
                return result;
            }
            if (result.FileError is not null)
                return result;

            // Empty rows were skipped silently during parsing; enforce the row limit.
            if (dataRows.Count > MaxRowsPerImport)
            {
                result.FileError = $"The spreadsheet contains {dataRows.Count} data rows. " +
                                   $"The maximum per import is {MaxRowsPerImport}. Please split the file and import again.";
                return result;
            }

            result.TotalRows = dataRows.Count;

            // ── 4. Load lookup data once per import ──────────────────────────
            var categoriesByName = await LoadCategoryLookupAsync();
            var municipalitiesByKey = await LoadMunicipalityLookupAsync();

            // ── 5. Process row by row ────────────────────────────────────────
            foreach (var row in dataRows)
            {
                var rowResult = await ProcessRowAsync(row, zipFolders, folderWarnings, categoriesByName, municipalitiesByKey);
                result.Rows.Add(rowResult);

                if (rowResult.Succeeded)
                    result.SuccessCount++;
                else
                    result.FailedCount++;
            }

            _logger.LogInformation(
                "Bulk import finished: {Success} created, {Failed} failed out of {Total} data rows.",
                result.SuccessCount, result.FailedCount, result.TotalRows);

            return result;
        }

        // ═══════════════════════ Row processing ═══════════════════════

        private async Task<BulkImportRowResultDTO> ProcessRowAsync(
            ParsedRow row,
            Dictionary<string, Dictionary<string, byte[]>>? zipFolders,
            Dictionary<string, List<string>> folderWarnings,
            Dictionary<string, Category> categoriesByName,
            Dictionary<string, Municipality> municipalitiesByKey)
        {
            try
            {
                // Required text field
                var title = row.Cells.GetValueOrDefault("Title");
                if (string.IsNullOrWhiteSpace(title))
                    return Fail(row, "Title is required.");

                // ReservePrice: required, positive number
                var reservePriceText = row.Cells.GetValueOrDefault("ReservePrice");
                if (string.IsNullOrWhiteSpace(reservePriceText))
                    return Fail(row, "ReservePrice is required.");
                if (!TryParseDecimal(reservePriceText, out var reservePrice) || reservePrice <= 0)
                    return Fail(row, $"ReservePrice '{reservePriceText}' is not a valid number greater than 0.");

                // Optional coordinates (same ranges as the Create form)
                var latitude = 0d;
                var longitude = 0d;
                var latitudeText = row.Cells.GetValueOrDefault("Latitude");
                if (!string.IsNullOrWhiteSpace(latitudeText) &&
                    (!TryParseDouble(latitudeText, out latitude) || latitude < -90 || latitude > 90))
                    return Fail(row, $"Latitude '{latitudeText}' must be a number between -90 and 90.");

                var longitudeText = row.Cells.GetValueOrDefault("Longitude");
                if (!string.IsNullOrWhiteSpace(longitudeText) &&
                    (!TryParseDouble(longitudeText, out longitude) || longitude < -180 || longitude > 180))
                    return Fail(row, $"Longitude '{longitudeText}' must be a number between -180 and 180.");

                // Dates: flexible parsing, but End must be after Start
                var startText = row.Cells.GetValueOrDefault("AuctionStartDate");
                if (string.IsNullOrWhiteSpace(startText))
                    return Fail(row, "AuctionStartDate is required.");
                if (!TryParseDate(startText, out var start))
                    return Fail(row, $"AuctionStartDate '{startText}' is not a recognised date. Use e.g. 2026-10-01 10:00 or 01/10/2026 10:00.");

                var endText = row.Cells.GetValueOrDefault("AuctionEndDate");
                if (string.IsNullOrWhiteSpace(endText))
                    return Fail(row, "AuctionEndDate is required.");
                if (!TryParseDate(endText, out var end))
                    return Fail(row, $"AuctionEndDate '{endText}' is not a recognised date. Use e.g. 2026-10-10 17:00 or 10/10/2026 17:00.");

                if (end <= start)
                    return Fail(row, "AuctionEndDate must be after AuctionStartDate.");

                // Status: only Draft or Active are accepted from the spreadsheet
                var statusText = row.Cells.GetValueOrDefault("Status");
                if (string.IsNullOrWhiteSpace(statusText))
                    return Fail(row, "Status is required.");
                if (!TryParseStatus(statusText, out var status))
                    return Fail(row, $"Status '{statusText}' is not valid. Only 'Draft' or 'Active' are accepted.");

                // Collateral category: display name, enum name or number
                var collateralText = row.Cells.GetValueOrDefault("CollateralCategory");
                if (string.IsNullOrWhiteSpace(collateralText))
                    return Fail(row, "CollateralCategory is required.");
                var collateral = CollateralCategoryExtensions.ParseCollateralCategory(collateralText);
                if (collateral is null)
                    return Fail(row, $"CollateralCategory '{collateralText}' is not recognised. Use Land, Residential Property, Commercial or Vehicle.");

                // Category lookup by name (active categories preferred)
                var categoryName = row.Cells.GetValueOrDefault("CategoryName");
                if (string.IsNullOrWhiteSpace(categoryName))
                    return Fail(row, "CategoryName is required.");
                if (!categoriesByName.TryGetValue(Normalize(categoryName), out var category))
                    return Fail(row, $"Category '{categoryName}' was not found. Use an existing category name from the admin category list.");

                // Location lookup: Province → District → Municipality (exact match, case-insensitive)
                var provinceName = row.Cells.GetValueOrDefault("ProvinceName");
                var districtName = row.Cells.GetValueOrDefault("DistrictName");
                var municipalityName = row.Cells.GetValueOrDefault("MunicipalityName");
                if (string.IsNullOrWhiteSpace(provinceName) || string.IsNullOrWhiteSpace(districtName) || string.IsNullOrWhiteSpace(municipalityName))
                    return Fail(row, "ProvinceName, DistrictName and MunicipalityName are all required.");
                var locationKey = $"{Normalize(provinceName)}|{Normalize(districtName)}|{Normalize(municipalityName)}";
                if (!municipalitiesByKey.TryGetValue(locationKey, out var municipality))
                    return Fail(row, $"Municipality '{municipalityName}' was not found under district '{districtName}' (province '{provinceName}'). " +
                                     "Check the spelling and make sure the municipality belongs to that district and province.");

                // ── Resolve this row's media folder (if any) ──
                // MediaFolder holds a plain number (e.g. "1") matching a subfolder
                // inside the uploaded ZIP. Every image/PDF found inside that
                // subfolder is attached automatically — no filenames need to be typed.
                var imageNames = new List<string>();
                var documentNames = new List<string>();
                Dictionary<string, byte[]>? folderFiles = null;

                var folderText = row.Cells.GetValueOrDefault("MediaFolder");
                if (!string.IsNullOrWhiteSpace(folderText))
                {
                    if (zipFolders is null)
                        return Fail(row, $"This row references media folder '{folderText}', but no ZIP archive was uploaded. " +
                                         "Upload the ZIP or clear the MediaFolder column.");

                    if (!int.TryParse(folderText.Trim(), out var folderNumber) || folderNumber <= 0)
                        return Fail(row, $"MediaFolder '{folderText}' must be a positive whole number matching a subfolder name in the ZIP (e.g. 1, 2, 3).");

                    var folderKey = folderNumber.ToString(CultureInfo.InvariantCulture);

                    if (!zipFolders.TryGetValue(folderKey, out folderFiles) || folderFiles.Count == 0)
                        return Fail(row, $"No usable files were found inside subfolder '{folderKey}' of the uploaded ZIP.");

                    foreach (var name in folderFiles.Keys)
                    {
                        var ext = Path.GetExtension(name).ToLowerInvariant();
                        if (AllowedImageExtensions.Contains(ext))
                            imageNames.Add(name);
                        else if (AllowedDocumentExtensions.Contains(ext))
                            documentNames.Add(name);
                    }
                }

                // ── Create the item through the same service as the single-item flow ──
                var dto = new AdminAuctionItemCreateDTO
                {
                    Title = title,
                    Description = row.Cells.GetValueOrDefault("Description") ?? string.Empty,
                    ReservePrice = reservePrice,
                    Latitude = latitude,
                    Longitude = longitude,
                    AuctionStartDate = start,
                    AuctionEndDate = end,
                    Status = status.GetValueOrDefault(),
                    CollateralCategory = collateral.GetValueOrDefault(),
                    CategoryId = category.Id,
                    MunicipalityId = municipality.Id
                };

                var createResult = await _adminService.CreateAsync(dto);
                if (!createResult.Succeeded)
                    return Fail(row, createResult.ErrorMessage ?? "The auction item could not be created.");

                int newItemId = createResult.Data;

                // ── Save the resolved media exactly like the single-item upload flow ──
                var warnings = new List<string>();

                if (!string.IsNullOrWhiteSpace(folderText) &&
                    folderWarnings.TryGetValue(int.Parse(folderText.Trim()).ToString(CultureInfo.InvariantCulture), out var skipped))
                {
                    warnings.AddRange(skipped);
                }

                if (folderFiles is not null)
                {
                    foreach (var name in imageNames)
                    {
                        var bytes = folderFiles[name];
                        var saved = await _uploadService.SaveStreamAsync(
                            newItemId, new MemoryStream(bytes), bytes.Length, name, FileType.Image);
                        if (!saved.Succeeded)
                            warnings.Add(saved.ErrorMessage!);
                    }

                    foreach (var name in documentNames)
                    {
                        var bytes = folderFiles[name];
                        var saved = await _uploadService.SaveStreamAsync(
                            newItemId, new MemoryStream(bytes), bytes.Length, name, FileType.PDFNotice);
                        if (!saved.Succeeded)
                            warnings.Add(saved.ErrorMessage!);
                    }
                }

                _logger.LogInformation(
                    "Bulk import row {Row}: created auction item {ItemId} ('{Title}').",
                    row.RowNumber, newItemId, title);

                var message = $"Created auction item #{newItemId}.";
                if (warnings.Count > 0)
                    message += " Warnings: " + string.Join(" ", warnings);

                return new BulkImportRowResultDTO
                {
                    RowNumber = row.RowNumber,
                    Title = title,
                    Succeeded = true,
                    CreatedItemId = newItemId,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk import row {Row} failed unexpectedly.", row.RowNumber);
                return Fail(row, "An unexpected error occurred while importing this row.");
            }
        }

        private static BulkImportRowResultDTO Fail(ParsedRow row, string message) => new()
        {
            RowNumber = row.RowNumber,
            Title = row.Cells.GetValueOrDefault("Title") ?? string.Empty,
            Succeeded = false,
            Message = message
        };

        // ═══════════════════════ Spreadsheet parsing ═══════════════════════

        private List<ParsedRow> ParseSpreadsheet(IFormFile spreadsheet, BulkImportResultDTO result)
        {
            using var stream = spreadsheet.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var worksheetXml = LoadFirstWorksheet(archive);
            if (worksheetXml is null)
            {
                result.FileError = "The spreadsheet is empty.";
                return new List<ParsedRow>();
            }

            var sharedStrings = LoadSharedStrings(archive);
            var worksheetRows = worksheetXml.Descendants(SpreadsheetNs + "row").ToList();
            if (worksheetRows.Count == 0)
            {
                result.FileError = "The spreadsheet is empty.";
                return new List<ParsedRow>();
            }

            // Case-insensitive header matching: header text → column number.
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headerRow = worksheetRows[0];
            foreach (var cell in headerRow.Elements(SpreadsheetNs + "c"))
            {
                var header = ReadCellText(cell, sharedStrings).Trim();
                if (header.Length > 0)
                    columnMap[header] = ColumnNumber((string?)cell.Attribute("r"));
            }

            var missing = RequiredHeaders.Where(h => !columnMap.ContainsKey(h)).ToList();
            if (missing.Count > 0)
            {
                result.FileError = "The spreadsheet is missing required column(s): " + string.Join(", ", missing) + ".";
                return new List<ParsedRow>();
            }

            var parsedRows = new List<ParsedRow>();
            foreach (var xmlRow in worksheetRows.Skip(1))
            {
                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var isEmpty = true;
                foreach (var (header, column) in columnMap)
                {
                    var cell = xmlRow.Elements(SpreadsheetNs + "c")
                        .FirstOrDefault(candidate => ColumnNumber((string?)candidate.Attribute("r")) == column);
                    var text = cell is null ? string.Empty : ReadCellText(cell, sharedStrings);
                    cells[header] = text;
                    if (text.Length > 0)
                        isEmpty = false;
                }

                // Empty rows are skipped silently.
                if (isEmpty)
                    continue;

                parsedRows.Add(new ParsedRow((int?)xmlRow.Attribute("r") ?? parsedRows.Count + 2, cells));
            }

            return parsedRows;
        }

        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        private static XDocument? LoadFirstWorksheet(ZipArchive archive)
        {
            var workbook = LoadXmlEntry(archive, "xl/workbook.xml");
            var relationships = LoadXmlEntry(archive, "xl/_rels/workbook.xml.rels");
            var sheet = workbook?.Descendants(SpreadsheetNs + "sheet").FirstOrDefault();
            var relationshipId = (string?)sheet?.Attribute(RelationshipNs + "id");
            var target = relationships?.Descendants(PackageRelationshipNs + "Relationship")
                .FirstOrDefault(item => (string?)item.Attribute("Id") == relationshipId)?.Attribute("Target")?.Value;
            if (string.IsNullOrWhiteSpace(target))
                return null;

            var entryName = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target.TrimStart('/');
            return LoadXmlEntry(archive, entryName);
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var document = LoadXmlEntry(archive, "xl/sharedStrings.xml");
            return document?.Descendants(SpreadsheetNs + "si")
                .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
                .ToList() ?? new List<string>();
        }

        private static XDocument? LoadXmlEntry(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name);
            if (entry is null)
                return null;
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }

        private static string ReadCellText(XElement cell, IReadOnlyList<string> sharedStrings)
        {
            var type = (string?)cell.Attribute("t");
            if (type == "inlineStr")
                return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value));

            var value = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
            if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];
            return value;
        }

        private static int ColumnNumber(string? cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return 0;

            var column = 0;
            foreach (var character in cellReference)
            {
                if (!char.IsLetter(character))
                    break;
                column = column * 26 + char.ToUpperInvariant(character) - 'A' + 1;
            }
            return column;
        }

        // ═══════════════════════ ZIP handling ═══════════════════════

        /// <summary>
        /// Loads every image/PDF entry from the ZIP into memory, grouped by its
        /// immediate parent folder name — which must be a plain number ("1", "2",
        /// "3"...). Any folder wrapping those numbered folders (e.g. a top-level
        /// "AuctionMedia/" the person zipped up) is ignored; only the last path
        /// segment before the file name matters. Files that sit directly at the
        /// ZIP root, inside a non-numbered folder, have an unsupported extension,
        /// or exceed the per-file size limit are skipped and recorded as a
        /// per-folder warning (or logged, if they can't be tied to a folder at all)
        /// rather than failing the whole import.
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, byte[]>>?> LoadZipAsync(
            IFormFile zipFile,
            Dictionary<string, List<string>> folderWarnings,
            BulkImportResultDTO result)
        {
            var folders = new Dictionary<string, Dictionary<string, byte[]>>(StringComparer.Ordinal);
            long totalUncompressed = 0;

            try
            {
                await using var stream = zipFile.OpenReadStream();
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in archive.Entries)
                {
                    var entryPath = entry.FullName.Replace('\\', '/').Trim('/');
                    if (string.IsNullOrWhiteSpace(entryPath) || entry.FullName.EndsWith("/"))
                        continue; // directory entry

                    var parts = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var fileName = Path.GetFileName(parts[^1]);
                    if (string.IsNullOrWhiteSpace(fileName))
                        continue;

                    // The immediate parent folder must be a plain positive number.
                    if (parts.Length < 2 || parts[^2].Length == 0 || !parts[^2].All(char.IsDigit))
                    {
                        _logger.LogWarning(
                            "Bulk import: ZIP entry '{Entry}' is not inside a numbered subfolder (e.g. \"1/{File}\") and was skipped.",
                            entryPath, fileName);
                        continue;
                    }

                    var folderKey = int.Parse(parts[^2], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

                    var ext = Path.GetExtension(fileName).ToLowerInvariant();
                    if (!AllowedImageExtensions.Contains(ext) && !AllowedDocumentExtensions.Contains(ext))
                    {
                        AddFolderWarning(folderWarnings, folderKey, $"'{fileName}' in folder '{folderKey}' has an unsupported file type and was skipped.");
                        continue;
                    }

                    if (entry.Length > MaxMediaFileBytes)
                    {
                        AddFolderWarning(folderWarnings, folderKey, $"'{fileName}' in folder '{folderKey}' exceeds the 10 MB per-file limit and was skipped.");
                        continue;
                    }

                    totalUncompressed += entry.Length;
                    if (totalUncompressed > MaxZipUncompressedBytes)
                    {
                        _logger.LogWarning("Bulk import: ZIP '{Zip}' is too large when uncompressed.", zipFile.FileName);
                        result.FileError = "The ZIP file is too large when uncompressed.";
                        return null;
                    }

                    if (!folders.TryGetValue(folderKey, out var filesInFolder))
                    {
                        filesInFolder = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                        folders[folderKey] = filesInFolder;
                    }

                    if (filesInFolder.ContainsKey(fileName))
                        continue; // duplicate name within the same folder — first one wins

                    using var entryStream = entry.Open();
                    using var buffer = new MemoryStream();
                    await entryStream.CopyToAsync(buffer);
                    filesInFolder[fileName] = buffer.ToArray();
                }

                return folders;
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning(ex, "Bulk import: '{Zip}' is not a valid ZIP archive.", zipFile.FileName);
                result.FileError = "The uploaded ZIP file is not a valid archive.";
                return null;
            }
        }

        private static void AddFolderWarning(Dictionary<string, List<string>> folderWarnings, string folderKey, string message)
        {
            if (!folderWarnings.TryGetValue(folderKey, out var list))
            {
                list = new List<string>();
                folderWarnings[folderKey] = list;
            }
            list.Add(message);
        }

        // ═══════════════════════ Lookups ═══════════════════════

        private async Task<Dictionary<string, Category>> LoadCategoryLookupAsync()
        {
            var categories = await _db.Categories.ToListAsync();
            var map = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

            foreach (var category in categories)
            {
                var key = Normalize(category.Name);
                if (key.Length == 0)
                    continue;

                // Prefer active categories when two categories share a name.
                if (map.TryGetValue(key, out var existing) &&
                    (existing.Active ?? true) &&
                    !(category.Active ?? true))
                    continue;

                map[key] = category;
            }

            return map;
        }

        private async Task<Dictionary<string, Municipality>> LoadMunicipalityLookupAsync()
        {
            var municipalities = await _db.Municipalities
                .Include(m => m.District).ThenInclude(d => d.Province)
                .ToListAsync();

            var map = new Dictionary<string, Municipality>(StringComparer.OrdinalIgnoreCase);
            foreach (var municipality in municipalities)
            {
                var key = $"{Normalize(municipality.District.Province.Name)}" +
                          $"|{Normalize(municipality.District.Name)}" +
                          $"|{Normalize(municipality.Name)}";
                map.TryAdd(key, municipality);
            }

            return map;
        }

        // ═══════════════════════ Template ═══════════════════════

        public byte[] GenerateTemplate()
        {
            var rows = new[]
            {
                Headers,
                new[] { "Sample Land Auction - Ward 5, Kathmandu", "2 ropani land with 20 ft road access near the main highway.", "1500000", "27.7172", "85.3240", "2026-10-01 10:00", "2026-10-10 17:00", "Active", "Land", "Land & Property", "Bagmati", "Kathmandu", "Kathmandu Metropolitan City", "1" },
                new[] { "Sample Vehicle Auction - Truck", "2019 model truck, 6 tyres, running condition.", "2200000", "", "", "2026-11-01 09:00", "2026-11-05 17:00", "Draft", "Vehicle", "Vehicles", "Bagmati", "Lalitpur", "Lalitpur Metropolitan City", "" }
            };

            var worksheet = new XElement(SpreadsheetNs + "worksheet",
                new XAttribute(XNamespace.Xmlns + "main", SpreadsheetNs),
                new XElement(SpreadsheetNs + "sheetData",
                    rows.Select((row, rowIndex) => new XElement(SpreadsheetNs + "row",
                        new XAttribute("r", rowIndex + 1),
                        row.Select((value, columnIndex) => CreateInlineCell(rowIndex + 1, columnIndex + 1, value))))));

            using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteZipEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                WriteZipEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                WriteZipEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"AuctionItems\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                WriteZipEntry(archive, "xl/worksheets/sheet1.xml", worksheet.ToString(SaveOptions.DisableFormatting));
            }
            return output.ToArray();
        }

        private static XElement CreateInlineCell(int row, int column, string value) =>
            new(SpreadsheetNs + "c", new XAttribute("r", $"{ColumnName(column)}{row}"), new XAttribute("t", "inlineStr"),
                new XElement(SpreadsheetNs + "is", new XElement(SpreadsheetNs + "t", value)));

        private static string ColumnName(int column)
        {
            var name = string.Empty;
            while (column > 0)
            {
                column--;
                name = (char)('A' + column % 26) + name;
                column /= 26;
            }
            return name;
        }

        private static void WriteZipEntry(ZipArchive archive, string name, string content)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        // ═══════════════════════ Small parsing helpers ═══════════════════════

        private static bool HasExtension(string fileName, string[] allowed) =>
            allowed.Contains(Path.GetExtension(fileName).ToLowerInvariant());

        private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

        private static bool TryParseStatus(string text, out AuctionStatus? status)
        {
            status = null;
            var normalized = text.Trim();

            if (normalized.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                status = AuctionStatus.Draft;
                return true;
            }
            if (normalized.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                status = AuctionStatus.Active;
                return true;
            }

            return false;
        }

        private static bool TryParseDate(string text, out DateTime value)
        {
            value = default;
            var input = text.Trim();

            foreach (var format in DateFormats)
            {
                if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                    return true;
            }

            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                return true;

            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out value))
                return true;

            // Raw Excel serial number
            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial) &&
                serial > 0 && serial < 100000)
            {
                try
                {
                    value = DateTime.FromOADate(serial);
                    return true;
                }
                catch (ArgumentException)
                {
                }
            }

            return false;
        }

        private static bool TryParseDecimal(string text, out decimal value)
        {
            value = 0;
            var input = text.Trim();

            // Tolerate simple currency prefixes and thousand separators.
            if (input.StartsWith("Rs.", StringComparison.OrdinalIgnoreCase))
                input = input[3..];
            else if (input.StartsWith("Rs", StringComparison.OrdinalIgnoreCase))
                input = input[2..];
            if (input.StartsWith("NPR", StringComparison.OrdinalIgnoreCase))
                input = input[3..];

            input = input.Replace(",", string.Empty).Replace(" ", string.Empty);

            return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseDouble(string text, out double value)
        {
            var input = text.Trim().Replace(",", string.Empty).Replace(" ", string.Empty);
            return double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}