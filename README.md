# 🏦 Auction Portal Clone

An ASP.NET Core MVC auction portal for publishing and discovering bank-owned collateral, managing auction listings, and placing verified bids.  
The application cleanly separates **public catalogue browsing**, **authenticated bidder actions**, and **protected bank-staff administration**.

---

## 🎯 Purpose and Goals

The project provides a practical foundation for a transparent digital auction workflow:

- Give visitors a searchable, filterable catalogue of available collateral
- Let registered users save listings and place bids after account verification
- Give bank staff a focused workspace for creating, editing, publishing, and removing auction items
- Keep authentication, authorization, validation, persistence, and file handling in established ASP.NET Core components
- Support location-aware discovery through province, district, and municipality data

---

## ✨ Features

### 🌐 Public Catalogue

- Browse auction listings and open detailed listing pages
- Search and filter by keyword, category, collateral type, location, and page
- Browse dedicated collateral views for land, residential property, commercial assets, and vehicles
- View listing images, documents, auction dates, reserve prices, location details, and resolved auction status
- Resize and recompress images on demand through ImageSharp.Web (with disk caching)

### 🔐 Accounts and Bidder Safety

- Register with email, password, name, and phone number
- Sign in with local credentials or Google OAuth (automatic registration for new Google users)
- Enforce strong Identity passwords and temporary lockout after repeated failures
- Verify an account for bidding via signed email link or matching Google account
- Use ASP.NET Core Identity cookies, antiforgery validation, and role-based authorization

### 👤 Authenticated Bidder Tools

- Place bids from an auction detail page
- Save and unsave listings
- View saved listings and retrieve wishlist data for the navigation UI
- View current user profile and bidding-verification state

### 🏆 Auction Settlement, Winner Determination & Notifications

- **Automatic Background Worker**: Periodic hosted service (`AuctionWinnerHostedService`) runs every 60 seconds (configurable) to scan and finalize ended auctions without requiring page visits.
- **Idempotent Winner Logic**: When an auction ends (`AuctionEndDate <= UtcNow`):
  - Qualifying bids must meet or exceed `ReservePrice`.
  - The highest qualifying bid is awarded the win (ties broken by earliest submission timestamp).
  - If no bids or no bids meet reserve, the asset is marked `Unsold` (No Winner).
  - Determinations are recorded atomically (`WinnerUserId`, `WinningBidId`, `WinningAmount`, `FinalStatus`, `WinnerDeterminedAt`) and are fully idempotent.
- **Winner Email Notification**: Winning bidder receives an official HTML notification via `IEmailSender` including asset reference, winning amount in NPR, claim instructions, bank branch location, and a 7-day claim window.
- **Bidder Experience**:
  - Details page displays clear outcome banners: 🎉 **You Won** (with claim next steps), **You Did Not Win** (with winning bid amount comparison), or **Auction Ended • No Winner**.
  - Bidder Profile includes a dedicated **My Bids & Outcomes** tracking section.

### 🛡️ Bank Staff Administration

Users in the `BankStaff` role can:

- View dashboard summary information
- Search and filter all auction items
- Access the **Auction Settlement & Winner Report** (`/Admin/Reports`) with KPI summary cards (Total Ended, Sold, Unsold, Realized Value, Bids)
- Filter ended auctions by date range, outcome (Sold/Unsold/Pending), collateral type, and keywords
- Trigger immediate settlement via the **Finalize Ended Auctions Now** manual fallback action
- Export ended auction records to **CSV** and generate clean **Print Views**
- Create and edit auction listings
- Manage draft and active listing intent (upcoming/closed states resolved from dates)
- Upload images and supporting documents
- Remove listing attachments
- Delete auction items
- Manage categories and Nepal location data through administration forms

### 🗄️ Data and Infrastructure

- Entity Framework Core with SQL Server
- Versioned database migrations in `Migrations/`
- Startup seeding for the `BankStaff` role, a local development administrator, categories, and location data
- Service and interface layers for catalogues, bids, saved listings, administration, attachments, email, and view data
- Responsive Razor views and client-side assets under `Views/` and `wwwroot/`

---

## 🧰 Technology Stack

| Technology                 | Purpose                                       |
| -------------------------- | --------------------------------------------- |
| .NET 10 + ASP.NET Core MVC | Application framework                         |
| C#                         | Primary language (nullable + implicit usings) |
| Entity Framework Core 10   | ORM + SQL Server                              |
| ASP.NET Core Identity      | Authentication & authorization                |
| Google Authentication      | OAuth login + bidding verification            |
| Razor Views + HTML/CSS/JS  | UI layer                                      |
| SixLabors ImageSharp.Web   | On-demand image resizing & caching            |
| Gmail SMTP                 | Bidding verification emails                   |

---

## 📁 Project Structure

```text
Auction Portal Clone/
├── Controllers/              # MVC endpoints and authorization boundaries
│   └── Admin/                # BankStaff-only dashboard and auction management
├── Data/                     # DbContext and startup data seeding
├── DTO/                      # Request, filter, list, detail, and response models
├── Migrations/               # EF Core schema history and model snapshot
├── Models/                   # Domain entities and enums
├── Services/
│   ├── Implementation/       # Application service implementations
│   └── Interfaces/           # Service contracts used by controllers
├── Views/                    # Razor pages grouped by feature
├── wwwroot/                  # CSS, JavaScript, images, uploads, and static libraries
├── Program.cs                # Dependency injection and HTTP pipeline configuration
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Example.json
├── todo.md
└── README.md
```

---

## ⚙️ Prerequisites

1. .NET 10 SDK
2. SQL Server or SQL Server Express / LocalDB
3. Google Cloud OAuth client _(if Google login or Google bidding verification is required)_
4. Gmail account with an app password _(if email verification is required)_

```powershell
dotnet --version
```

---

## 🔧 Configuration

Do **not** put database passwords, OAuth secrets, or Gmail app passwords in source control.  
Use .NET User Secrets for local development.

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=AuctionPortalClone;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
dotnet user-secrets set "EmailSettings:GmailAddress" "your-gmail-address"
dotnet user-secrets set "EmailSettings:GmailAppPassword" "your-gmail-app-password"
```

`appsettings.Example.json` contains the same configuration shape with placeholders.  
Copy its values into User Secrets or environment variables — never into `appsettings.json`.

### Enable Google Login

Google login and Google bidding verification are enabled automatically when both Google values are present in User Secrets.  
The application intentionally keeps the values in `appsettings.Development.json` empty so OAuth credentials are not committed.

If credentials were previously exposed, revoke the old OAuth client secret in Google Cloud Console and create a replacement, then set the new values with `dotnet user-secrets`.

### Google OAuth Callback URLs (local)

```text
https://localhost:7103/signin-google
http://localhost:5065/signin-google
```

Restrict Google OAuth credentials to the required origins and callback URLs.

---

## 🗄️ Database Setup

```powershell
dotnet restore
dotnet ef database update
```

On first startup the application seeds:

- `BankStaff` role
- Development administrator: `admin@auctionportal.local` / `Admin@12345`
- Initial categories and province/district/municipality records

> **Important:** Change the seeded administrator password immediately after first login.

To create a new migration:

```powershell
dotnet ef migrations add DescribeYourChange
dotnet ef database update
```

---

## 🚀 Run Locally

```powershell
dotnet run
```

Available URLs:

- `https://localhost:7103`
- `http://localhost:5065`

Trust the HTTPS development certificate if needed:

```powershell
dotnet dev-certs https --trust
```

---

## 🗺️ Useful Routes

| Area                | Route                          | Access                                 |
| ------------------- | ------------------------------ | -------------------------------------- |
| Home                | `/`                            | Public                                 |
| Catalogue           | `/AuctionCatalog`              | Public                                 |
| Listing details     | `/AuctionCatalog/Details/{id}` | Public                                 |
| Register            | `/Register`                    | Anonymous                              |
| Login               | `/Login`                       | Anonymous                              |
| Saved listings      | `/SavedListing`                | Authenticated                          |
| Profile             | `/profile`                     | Public page; protected profile actions |
| Admin dashboard     | `/Admin/Dashboard`             | `BankStaff`                            |
| Admin auction items | `/Admin/AuctionItem`           | `BankStaff`                            |
| Admin winner report | `/Admin/Reports`               | `BankStaff`                            |

---

## ⚡ Auction Winner Settlement & How to Test It

### ⚙️ How Winner Determination Operates

1. **Background Service (`AuctionWinnerHostedService`)**:
   - Runs continuously in the background on a configurable timer (`AuctionSettings:WinnerCheckIntervalSeconds`, default: 60s).
   - Scans for auction listings that meet all three conditions:
     - `Status != AuctionStatus.Draft`
     - `AuctionEndDate <= UtcNow`
     - `FinalStatus == AuctionFinalStatus.Pending` (`WinnerDeterminedAt == null`)
2. **Winner Selection Logic (`AuctionWinnerService`)**:
   - Compares all bids placed on the item where `OfferedAmount >= ReservePrice`.
   - Highest `OfferedAmount` wins. If two bidders bid the same highest amount, the earlier `SubmittedAt` takes precedence.
   - If one or more bids meet or exceed the reserve price:
     - Sets `FinalStatus = Sold` and `Status = Closed`.
     - Sets `WinnerUserId`, `WinningBidId`, `WinningAmount`, and `WinnerDeterminedAt`.
     - Dispatches a formatted HTML award email to the winner's registered address via `IEmailSender`.
     - Marks `WinnerNotifiedAt` to prevent duplicate emails.
   - If no bids were submitted or no bids reached the reserve price:
     - Sets `FinalStatus = Unsold` and `Status = Closed`.
     - Sets `WinnerDeterminedAt` with null winner.
3. **Idempotency & Concurrency Safety**:
   - Double-invocations or parallel threads are prevented via DB-state checking and a synchronization semaphore. Already-determined auctions are skipped.
4. **Manual Trigger Fallback**:
   - An administrator logged in as `BankStaff` can click **"Finalize Ended Auctions Now"** from `/Admin/Reports` to trigger immediate evaluation without waiting for the background timer.

### 🧪 Step-by-Step Local Testing Guide

#### Scenario 1: Successful Auction with Winner
1. Log in as an administrator (`admin@auctionportal.local` / `Admin@12345`).
2. Go to **Auction Manager** (`/Admin/AuctionItem`) and create a new auction listing:
   - Start Date: Past date/time (e.g. yesterday).
   - End Date: 1–2 minutes in the future (or a past time).
   - Reserve Price: `Rs. 500,000`.
   - Status: `Active`.
3. Log out and register or log in as a regular verified bidder (e.g. `bidder1@test.local`).
4. Navigate to the auction Details page and submit a bid higher than the reserve (e.g., `Rs. 550,000`).
5. (Optional) Log in as a second verified bidder and place a bid of `Rs. 520,000`.
6. Allow the end date to elapse. Within 60 seconds (or immediately by clicking **"Finalize Ended Auctions Now"** under `/Admin/Reports`), the background worker finalizes the auction.
7. **Verification**:
   - Open `/Admin/Reports`: The auction is listed as **Sold** with the winning bidder name, email, and amount `Rs. 550,000`.
   - Log in as the winning bidder and view the auction Details page: A celebration card displays **"🎉 You Won This Auction!"** with claim instructions.
   - Log in as the second bidder: The Details page displays **"You Did Not Win"** with the winning bid amount.
   - In the user profile (`/profile`), the **My Bids & Outcomes** section displays the **Won (Rs. 550,000)** or **Did Not Win** status badge.

#### Scenario 2: Unsold Auction (Reserve Not Met)
1. Create an active auction with Reserve Price `Rs. 1,000,000` and an End Date set to the past.
2. If any bids are below `Rs. 1,000,000` (or if no bids were submitted), trigger finalization.
3. **Verification**:
   - `/Admin/Reports` lists the auction as **Unsold**.
   - Public Details page displays **"Auction Ended • No Winner"** (reserve price not met).

---

## 🔒 Security Notes

- Treat all values in `appsettings.Development.json` and User Secrets as sensitive when they contain credentials
- Rotate any Google or Gmail credential that has been exposed
- Use HTTPS outside local development
- Replace the seeded development admin password before using real data
- Validate upload size, type, storage location, and access policy before production deployment
- Configure a production database, durable private file storage, email provider, logging, backups, and error monitoring

---

## 🛠️ Development Notes

The project follows a **service-oriented MVC structure**:

- Controllers handle HTTP concerns
- Services contain application behavior
- DTOs define view/request contracts
- EF Core models represent persisted data

Keep new business rules in services where possible so they remain testable and are not coupled to Razor or HTTP concerns.

Before opening a pull request:

```powershell
dotnet build
dotnet test
```

> There is currently no test project in the repository. High-value future coverage includes bid validation, auction status transitions, authorization boundaries, attachment validation, and verification-token flows.

---

## 📄 License

No license has been declared yet.  
Add a license before distributing or deploying this project outside its intended private development context.
