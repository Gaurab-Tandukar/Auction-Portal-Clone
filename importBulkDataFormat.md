# Auction Bulk Import — Data Format Instructions

You are generating rows for an "Auction Items" bulk import Excel/CSV file. Each row is one auction item. Follow these rules exactly — the importer validates strictly and will reject rows that don't match.

## Columns (in this exact order)

| # | Column | Type | Required | Rules |
|---|--------|------|----------|-------|
| 1 | Title | Text | Yes | Short, human-readable name of the item, e.g. `2020 Hyundai Creta SUV - Tikapur`. Include the item type and a place name if useful. |
| 2 | Description | Text | Yes | 1–3 sentences describing condition, key facts, specs. |
| 3 | ReservePrice | Number | Yes | Plain integer, no currency symbol or commas (e.g. `1200000`). |
| 4 | Latitude | Decimal number | No | Optional. Valid latitude for Nepal (roughly 26.3 to 30.5). Leave blank if unknown. |
| 5 | Longitude | Decimal number | No | Optional. Valid longitude for Nepal (roughly 80.0 to 88.2). Leave blank if unknown. |
| 6 | AuctionStartDate | DateTime | Yes | Format `YYYY-MM-DD HH:MM` (24-hour clock), e.g. `2026-11-13 15:00`. |
| 7 | AuctionEndDate | DateTime | Yes | Same format as above. Must be after AuctionStartDate. |
| 8 | Status | Enum | Yes | One of: `Draft`, `Active`. |
| 9 | CollateralCategory | Enum | Yes | One of: `Land`, `Residential Property`, `Commercial`, `Vehicle`. |
| 10 | CategoryName | Enum | Yes | One of: `Real Estate`, `Vehicles`. (`Real Estate` pairs with Land/Residential Property/Commercial; `Vehicles` pairs with Vehicle.) |
| 11 | ProvinceName | Enum | Yes | Must be one of Nepal's 7 official province names, spelled exactly as: `Koshi Province`, `Madhesh Province`, `Bagmati Province`, `Gandaki Province`, `Lumbini Province`, `Karnali Province`, `Sudurpaschim Province`. |
| 12 | DistrictName | Enum, depends on Province | Yes | Must be one of Nepal's 77 district names, and must actually belong to the Province chosen in column 11. |
| 13 | MunicipalityName | Enum, depends on District | Yes | Must be one of Nepal's 753 local-level names, and must actually belong to the District chosen in column 12. **Plain name only — do NOT append "Municipality", "Metropolitan City", "Sub-Metropolitan City", or "Rural Municipality".** e.g. use `Pokhara`, not `Pokhara Metropolitan City`; use `Dhangadhi`, not `Dhangadhi Sub-Metropolitan City`. |
| 14 | ImageFileNames | Text | No | Comma-separated list of filenames, e.g. `item10-photo1.jpg, item10-photo2.jpg`. Leave blank if none. |
| 15 | DocumentFileNames | Text | No | Comma-separated list of filenames, e.g. `item10-notice.pdf`. Leave blank if none. |

## Critical validation rules
- **Province / District / Municipality form a strict hierarchy.** A row fails if the District doesn't belong to the Province, or the Municipality doesn't belong to the District. Never invent or guess a location combination — only use real, verified Nepal administrative units.
- **MunicipalityName must never include a type suffix.** The importer stores only the bare name; adding "Municipality" etc. will cause a "not found" error even if the name is otherwise correct.
- Use exact official spelling/casing for Province and District names (e.g. `Sudurpaschim Province`, not `Sudurpashchim Province`).
- Dates must be chronologically valid and in `YYYY-MM-DD HH:MM` format.
- Don't leave any Required column blank.
