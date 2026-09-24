# Auction Bulk Import — Data Format Instructions

You are generating rows for an "Auction Items" bulk import Excel/CSV file. Each row is one auction item. Follow these rules exactly — the importer validates strictly and will reject rows that don't match.

## Columns (in this exact order)

| #   | Column             | Type                      | Required | Rules                                                                                                                                                                                                                                                                                                                                               |
| --- | ------------------ | ------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | Title              | Text                      | Yes      | Short, human-readable name of the item, e.g. `2020 Hyundai Creta SUV - Tikapur`. Include the item type and a place name if useful.                                                                                                                                                                                                                  |
| 2   | Description        | Text                      | Yes      | 1–3 sentences describing condition, key facts, specs.                                                                                                                                                                                                                                                                                               |
| 3   | ReservePrice       | Number                    | Yes      | Plain integer, no currency symbol or commas (e.g. `1200000`).                                                                                                                                                                                                                                                                                       |
| 4   | Latitude           | Decimal number            | No       | Optional. Valid latitude for Nepal (roughly 26.3 to 30.5). Leave blank if unknown.                                                                                                                                                                                                                                                                  |
| 5   | Longitude          | Decimal number            | No       | Optional. Valid longitude for Nepal (roughly 80.0 to 88.2). Leave blank if unknown.                                                                                                                                                                                                                                                                 |
| 6   | AuctionStartDate   | DateTime                  | Yes      | Format `YYYY-MM-DD HH:MM` (24-hour clock), e.g. `2026-11-13 15:00`.                                                                                                                                                                                                                                                                                 |
| 7   | AuctionEndDate     | DateTime                  | Yes      | Same format as above. Must be after AuctionStartDate.                                                                                                                                                                                                                                                                                               |
| 8   | Status             | Enum                      | Yes      | One of: `Draft`, `Active`.                                                                                                                                                                                                                                                                                                                          |
| 9   | CollateralCategory | Enum                      | Yes      | One of: `Land`, `Residential Property`, `Commercial`, `Vehicle`.                                                                                                                                                                                                                                                                                    |
| 10  | CategoryName       | Enum                      | Yes      | One of: `Real Estate`, `Vehicles`. (`Real Estate` pairs with Land/Residential Property/Commercial; `Vehicles` pairs with Vehicle.)                                                                                                                                                                                                                  |
| 11  | ProvinceName       | Enum                      | Yes      | Must be one of Nepal's 7 official province names, spelled exactly as: `Koshi Province`, `Madhesh Province`, `Bagmati Province`, `Gandaki Province`, `Lumbini Province`, `Karnali Province`, `Sudurpaschim Province`.                                                                                                                                |
| 12  | DistrictName       | Enum, depends on Province | Yes      | Must be one of Nepal's 77 district names, and must actually belong to the Province chosen in column 11.                                                                                                                                                                                                                                             |
| 13  | MunicipalityName   | Enum, depends on District | Yes      | Must be one of Nepal's 753 local-level names, and must actually belong to the District chosen in column 12. **Plain name only — do NOT append "Municipality", "Metropolitan City", "Sub-Metropolitan City", or "Rural Municipality".** e.g. use `Pokhara`, not `Pokhara Metropolitan City`; use `Dhangadhi`, not `Dhangadhi Sub-Metropolitan City`. |
| 14  | MediaFolder        | Number                    | No       | A single positive whole number (e.g. `1`, `2`, `3`), matching a subfolder in the uploaded media ZIP. Leave blank if this row has no photos or documents. Do **not** put filenames here — see "Media ZIP structure" below.                                                                                                                           |

## Media ZIP structure

The old per-file `ImageFileNames`/`DocumentFileNames` columns are gone. Instead, the media ZIP must contain one **numbered subfolder per row** — the number matches that row's `MediaFolder` value:

```
media.zip
└── 1/
│     photo1.jpg
│     photo2.png
│     notice.pdf
└── 2/
│     photo1.jpg
└── 3/
      notice.pdf
```

- Every image (`.jpg`, `.jpeg`, `.png`, `.webp`) and PDF found inside a row's numbered folder is attached to that item automatically — file type is detected by extension, not by filename.
- Any wrapping top-level folder you zip everything inside (e.g. `MyExport/1/photo.jpg`) is fine and ignored; only the folder name immediately above each file has to be the plain number.
- Two rows must never point at the same folder number unless they're genuinely meant to share the exact same media.
- If `MediaFolder` is filled in but the ZIP has no matching numbered folder (or no ZIP was uploaded at all), that row fails.

## Critical validation rules

- **Province / District / Municipality form a strict hierarchy.** A row fails if the District doesn't belong to the Province, or the Municipality doesn't belong to the District. Never invent or guess a location combination — only use real, verified Nepal administrative units.
- **MunicipalityName must never include a type suffix.** The importer stores only the bare name; adding "Municipality" etc. will cause a "not found" error even if the name is otherwise correct.
- Use exact official spelling/casing for Province and District names (e.g. `Sudurpaschim Province`, not `Sudurpashchim Province`).
- **CategoryName must be an existing category in the system** — currently only `Real Estate` and `Vehicles`. Don't invent category names (e.g. `Land & Property`, `Commercial Property` do not exist).
- Dates must be chronologically valid and in `YYYY-MM-DD HH:MM` format.
- `MediaFolder`, if present, must be a positive whole number and must match an actual subfolder in the ZIP.
- Don't leave any Required column blank.
