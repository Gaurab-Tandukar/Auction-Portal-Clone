using Auction_Portal_Clone.Data;
using Auction_Portal_Clone.Models;
using Auction_Portal_Clone.Services.Implementation;
using Auction_Portal_Clone.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Web.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
const long MaxRequestBytes = 524288000; // 500 MB

builder.Services.AddDbContext<AuctionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuctionFilterService, AuctionFilterService>();
builder.Services.AddScoped<IAuctionCatalogService, AuctionCatalogService>();
builder.Services.AddScoped<ISavedListingService, SavedListingService>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IAdminAuctionItemService, AdminAuctionItemService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAttachmentUploadService, AttachmentUploadService>();
builder.Services.AddScoped<IBulkAuctionImportService, BulkAuctionImportService>();
builder.Services.AddScoped<IAdminViewDataHelper, AdminViewDataHelper>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
builder.Services.AddScoped<IAuctionWinnerService, AuctionWinnerService>();
builder.Services.AddScoped<IAuctionReportService, AuctionReportService>();
builder.Services.AddHostedService<AuctionWinnerHostedService>();

// ─── ImageSharp.Web: on-the-fly image resizing/compression ───
// Serves resized/recompressed variants via query string (?width=900&quality=75&format=webp)
// for any image under wwwroot, with automatic disk caching so each variant is only
// processed once. Requires: dotnet add package SixLabors.ImageSharp.Web
builder.Services.AddImageSharp(options =>
{
    options.BrowserMaxAge = TimeSpan.FromDays(30); // header sent to browsers
    options.CacheMaxAge = TimeSpan.FromDays(365);  // how long processed files stay in its disk cache
});

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuctionDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.CallbackPath = "/signin-google";
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.SaveTokens = true;
        });
}

// Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxRequestBytes;
});

// IIS (in-process / out-of-process)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = MaxRequestBytes;
});

// Multipart form parsing
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxRequestBytes;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Create/update the database schema, then seed roles and a default admin user
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

    // Applies all pending EF migrations (creates the database and tables if missing).
    // Requires a Migrations folder in the project (Add-Migration InitialCreate).
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    string[] roles = { "BankStaff" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    const string adminEmail = "admin@auctionportal.local";
    const string adminPassword = "Admin@12345"; // change after first login

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Bank Admin",
            EmailConfirmed = true,
            IsVerifiedForBidding = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "BankStaff");
        }
    }
    else if (!await userManager.IsInRoleAsync(adminUser, "BankStaff"))
    {
        await userManager.AddToRoleAsync(adminUser, "BankStaff");
    }

    await DbSeeder.SeedInitialDataAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Beanstalk environment only listens on HTTP (port 80), so skip the HTTPS redirect there.
// Remove this condition once you add an HTTPS listener/certificate to the load balancer.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// IMPORTANT: UseImageSharp() must run before static files are served, so it can
// intercept image requests and return a resized/recompressed variant instead of
// the original file.
app.UseImageSharp();

// repeat-visit caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            ctx.Context.Response.Headers.CacheControl =
                "public,max-age=2592000"; // 30 days
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();