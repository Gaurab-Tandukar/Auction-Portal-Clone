# Auction Portal Clone

An ASP.NET Core MVC auction portal for publishing and discovering bank-owned collateral, managing auction listings, and placing verified bids. The application is designed around a clear separation between public catalogue browsing, authenticated bidder actions, and protected bank-staff administration.

## Purpose and Goals

The project provides a practical foundation for a transparent digital auction workflow:

- Give visitors a searchable, filterable catalogue of available collateral.
- Let registered users save listings and place bids after account verification.
- Give bank staff a focused workspace for creating, editing, publishing, and removing auction items.
- Keep authentication, authorization, validation, persistence, and file handling in established ASP.NET Core components.
- Support location-aware discovery through province, district, and municipality data.

## Features

### Public catalogue

- Browse auction listings and open detailed listing pages.
- Search and filter by keyword, category, collateral type, location, and page.
- Browse dedicated collateral views for land, residential property, commercial assets, and vehicles.
- View listing images, documents, auction dates, reserve prices, location details, and resolved auction status.
- Resize and recompress images on demand through ImageSharp.Web, with disk caching for generated variants.

### Accounts and bidder safety

- Register with an email address, password, name, and phone number.
- Sign in with local credentials or Google OAuth.
- Automatically register new users who sign in through Google.
- Enforce strong Identity passwords and temporary lockout after repeated failures.
- Verify an account for bidding through a signed email link or a matching Google account.
- Use ASP.NET Core Identity cookies, antiforgery validation, and role-based authorization.

### Authenticated bidder tools

- Place bids from an auction detail page.
- Save and unsave listings.
- View saved listings and retrieve wishlist data for the navigation UI.
- View the current user profile and bidding-verification state.

### Bank staff administration

Users in the `BankStaff` role can:

- View dashboard summary information.
- Search and filter all auction items.
- Create and edit auction listings.
- Manage draft and active listing intent while the application resolves upcoming and closed states from dates.
- Upload images and supporting documents.
- Remove listing attachments.
- Delete auction items.
- Manage categories and Nepal location data through the administration forms.

### Data and infrastructure

- Entity Framework Core with SQL Server.
- Versioned database migrations in `Migrations/`.
- Startup seeding for the `BankStaff` role, a local development administrator, categories, and location data.
- Service and interface layers for catalogues, bids, saved listings, administration, attachments, email, and view data.
- Responsive Razor views and client-side assets under `Views/` and `wwwroot/`.

## Technology Stack

- .NET 10 and ASP.NET Core MVC
- C# with nullable reference types and implicit usings enabled
- Entity Framework Core 10 with SQL Server
- ASP.NET Core Identity and Google Authentication
- Razor Views, HTML, CSS, and JavaScript
- SixLabors ImageSharp.Web
- Gmail SMTP for bidding-verification email

## Project Structure

```text
Auction Portal Clone/
├── Controllers/              MVC endpoints and authorization boundaries
│   └── Admin/                 BankStaff-only dashboard and auction management
├── Data/                      DbContext and startup data seeding
├── DTO/                       Request, filter, list, detail, and response models
├── Migrations/                EF Core schema history and model snapshot
├── Models/                    Domain entities and enums
├── Services/
│   ├── Implementation/       Application service implementations
│   └── Interfaces/           Service contracts used by controllers
├── Views/                     Razor pages grouped by feature
├── wwwroot/                   CSS, JavaScript, images, uploads, and static libraries
├── Program.cs                 Dependency injection and HTTP pipeline configuration
├── appsettings.json           Shared, non-secret configuration
├── appsettings.Development.json Development-only configuration overrides
├── appsettings.Example.json  Safe configuration template for local setup
├── todo.md                    Current product notes and follow-up work
└── README.md                 Project documentation
```

## Prerequisites

Install the following before running the application:

1. .NET 10 SDK.
2. SQL Server or SQL Server Express/LocalDB.
3. A Google Cloud OAuth client if Google login or Google bidding verification is required.
4. A Gmail account with an app password if email verification is required.

Check the SDK with:

```powershell
dotnet --version
```

## Configuration

Do not put database passwords, OAuth secrets, or Gmail app passwords in source control. The application reads these values from normal ASP.NET Core configuration, so local development can use .NET User Secrets.

From the project directory:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=AuctionPortalClone;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
dotnet user-secrets set "EmailSettings:GmailAddress" "your-gmail-address"
dotnet user-secrets set "EmailSettings:GmailAppPassword" "your-gmail-app-password"
```

`appsettings.Example.json` contains the same configuration shape with placeholders. Copy its values into User Secrets or environment variables; do not copy secrets into `appsettings.json`.

### Enable Google login

Google login and Google bidding verification are enabled automatically when both Google values are present in User Secrets. The application intentionally keeps the values in `appsettings.Development.json` empty so OAuth credentials are not committed to source control.

If credentials were previously stored in a repository or shared development file, revoke that OAuth client secret in Google Cloud Console and create a replacement before continuing. Then run these commands locally, replacing the placeholders with the new values:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "NEW_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "NEW_CLIENT_SECRET"
dotnet watch run
```

After restarting, the **Continue with Google** and **Sign up with Google** controls will be visible on the login and registration pages. Without both values, email/password authentication remains available and Google controls are hidden by design.

### Google OAuth callback URLs

For local development, register the callback URL that matches the profile you use:

```text
https://localhost:7103/signin-google
http://localhost:5065/signin-google
```

Google OAuth credentials should be restricted to the required origins and callback URLs.

## Database Setup

Restore dependencies and apply the existing migrations:

```powershell
dotnet restore
dotnet ef database update
```

The application also seeds the following on startup when they do not exist:

- The `BankStaff` role.
- Development administrator: `admin@auctionportal.local`.
- Development administrator password: `Admin@12345`.
- Initial categories and province/district/municipality records.

Change the seeded administrator password immediately after first login. For a real deployment, replace the startup credentials with a managed provisioning process.

To create a new migration after changing the data model:

```powershell
dotnet ef migrations add DescribeYourChange
dotnet ef database update
```

The EF command-line tool can be installed globally if needed:

```powershell
dotnet tool install --global dotnet-ef
```

## Run Locally

From the project directory:

```powershell
dotnet run
```

Available launch URLs are:

- `https://localhost:7103`
- `http://localhost:5065`

The first startup creates required roles and seed data. The HTTPS development certificate may need to be trusted once on a new machine:

```powershell
dotnet dev-certs https --trust
```

## Useful Routes

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

## Security Notes

- Treat all values in `appsettings.Development.json` and User Secrets as sensitive when they contain credentials.
- Rotate any Google or Gmail credential that has been exposed in a repository or shared development folder.
- Use HTTPS outside local development.
- Replace the seeded development admin password before using the application with real data.
- Validate upload size, type, storage location, and access policy before production deployment.
- Configure a production database, durable private file storage, email provider, logging, backups, and error monitoring.

## Development Notes

The project follows a service-oriented MVC structure: controllers handle HTTP concerns, services contain application behavior, DTOs define view/request contracts, and EF Core models represent persisted data. Keep new business rules in services where possible so they remain testable and are not coupled to Razor or HTTP concerns.

Before opening a pull request, run:

```powershell
dotnet build
dotnet test
```

There is currently no test project in the repository, so `dotnet test` will complete without executing application tests until one is added. High-value future coverage includes bid validation, auction status transitions, authorization boundaries, attachment validation, and verification-token flows.

## License

No license has been declared yet. Add a license before distributing or deploying this project outside its intended private development context.
