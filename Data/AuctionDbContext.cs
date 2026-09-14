using Auction_Portal_Clone.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auction_Portal_Clone.Data
{
      public class AuctionDbContext : IdentityDbContext<User>
      {
            public AuctionDbContext(DbContextOptions<AuctionDbContext> options)
                : base(options)
            {
            }

            public DbSet<Category> Categories { get; set; } = null!;
            public DbSet<AuctionItem> AuctionItems { get; set; } = null!;
            public DbSet<ItemAttachment> ItemAttachments { get; set; } = null!;
            public DbSet<Bid> Bids { get; set; } = null!;
            public DbSet<SavedListing> SavedListings { get; set; } = null!;
            public DbSet<Province> Provinces { get; set; } = null!;
            public DbSet<District> Districts { get; set; } = null!;
            public DbSet<Municipality> Municipalities { get; set; } = null!;

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                  base.OnModelCreating(modelBuilder); // Configures base ASP.NET Core Identity schema

                  // 1. User Entity Configuration
                  modelBuilder.Entity<User>(entity =>
                  {
                        entity.Property(u => u.FullName)
                        .IsRequired()
                        .HasMaxLength(100);

                        entity.Property(u => u.IsVerifiedForBidding)
                        .HasDefaultValue(false);

                        entity.Property(u => u.RegisteredAt)
                        .HasDefaultValueSql("GETUTCDATE()");
                  });

                  // 2. Category Entity Configuration
                  modelBuilder.Entity<Category>(entity =>
                  {
                        entity.HasKey(c => c.Id);

                        entity.Property(c => c.Name)
                        .IsRequired()
                        .HasMaxLength(100);

                        entity.Property(c => c.Active)
                        .IsRequired()
                        .HasDefaultValue(true);
                  });

                  // 3. AuctionItem Entity Configuration
                  modelBuilder.Entity<AuctionItem>(entity =>
                  {
                        entity.HasKey(a => a.Id);

                        entity.Property(a => a.Title)
                        .IsRequired()
                        .HasMaxLength(200);

                        entity.Property(a => a.Description)
                        .IsRequired();

                        entity.Property(a => a.ReservePrice)
                        .HasColumnType("decimal(18,2)")
                        .IsRequired();

                        entity.Property(a => a.Status)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(a => a.CollateralCategory)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(a => a.FinalStatus)
                        .HasConversion<int>()
                        .IsRequired()
                        .HasDefaultValue(AuctionFinalStatus.Pending);

                        entity.Property(a => a.WinningAmount)
                        .HasColumnType("decimal(18,2)");

                        // Relationship: Category -> AuctionItems (Restrict Delete)
                        entity.HasOne(a => a.Category)
                        .WithMany(c => c.AuctionItems)
                        .HasForeignKey(a => a.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Relationship: Municipality -> AuctionItems (Restrict Delete)
                        entity.HasOne(a => a.Municipality)
                        .WithMany(m => m.AuctionItems)
                        .HasForeignKey(a => a.MunicipalityId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Relationship: WinnerUser -> AuctionItems (Restrict Delete)
                        entity.HasOne(a => a.WinnerUser)
                        .WithMany()
                        .HasForeignKey(a => a.WinnerUserId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Relationship: WinningBid -> AuctionItems (Restrict Delete)
                        entity.HasOne(a => a.WinningBid)
                        .WithMany()
                        .HasForeignKey(a => a.WinningBidId)
                        .OnDelete(DeleteBehavior.Restrict);

                        // Indexing for search & filtering performance
                        entity.HasIndex(a => new { a.CategoryId, a.Status, a.AuctionEndDate });
                        entity.HasIndex(a => new { a.CollateralCategory, a.Status, a.AuctionEndDate });
                        entity.HasIndex(a => new { a.FinalStatus, a.AuctionEndDate });
                        entity.HasIndex(a => a.MunicipalityId);
                  });

                  // 4. ItemAttachment Entity Configuration
                  modelBuilder.Entity<ItemAttachment>(entity =>
                  {
                        entity.HasKey(ia => ia.Id);

                        entity.Property(ia => ia.FileUrl)
                        .IsRequired()
                        .HasMaxLength(2048);

                        entity.Property(ia => ia.FileName)
                        .HasMaxLength(100);

                        entity.Property(ia => ia.FileType)
                        .HasConversion<int>()
                        .IsRequired();

                        // Relationship: AuctionItem -> ItemAttachments (Cascade Delete)
                        entity.HasOne(ia => ia.AuctionItem)
                        .WithMany(a => a.Attachments)
                        .HasForeignKey(ia => ia.AuctionItemId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // 5. Bid Entity Configuration
                  modelBuilder.Entity<Bid>(entity =>
                  {
                        entity.HasKey(b => b.Id);

                        entity.Property(b => b.OfferedAmount)
                        .HasColumnType("decimal(18,2)")
                        .IsRequired();

                        entity.Property(b => b.SubmittedAt)
                        .HasDefaultValueSql("GETUTCDATE()");

                        // Relationship: User -> Bids (Cascade Delete)
                        entity.HasOne(b => b.User)
                        .WithMany(u => u.Bids)
                        .HasForeignKey(b => b.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                        // Relationship: AuctionItem -> Bids (Cascade Delete)
                        entity.HasOne(b => b.AuctionItem)
                        .WithMany(a => a.Bids)
                        .HasForeignKey(b => b.AuctionItemId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // 6. SavedListing Entity Configuration
                  modelBuilder.Entity<SavedListing>(entity =>
                  {
                        entity.HasKey(s => s.Id);

                        entity.Property(s => s.SavedAt)
                        .HasDefaultValueSql("GETUTCDATE()");

                        // Unique constraint: A user can save a given property only once
                        entity.HasIndex(s => new { s.UserId, s.AuctionItemId })
                        .IsUnique();

                        // Relationship: User -> SavedListings (Cascade Delete)
                        entity.HasOne(s => s.User)
                        .WithMany(u => u.SavedListings)
                        .HasForeignKey(s => s.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

                        // Relationship: AuctionItem -> SavedListings (Cascade Delete)
                        entity.HasOne(s => s.AuctionItem)
                        .WithMany(a => a.SavedListings)
                        .HasForeignKey(s => s.AuctionItemId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // 7. Province Entity Configuration
                  modelBuilder.Entity<Province>(entity =>
                  {
                        entity.HasKey(p => p.Id);

                        entity.Property(p => p.Name)
                        .IsRequired()
                        .HasMaxLength(100);
                  });

                  // 8. District Entity Configuration
                  modelBuilder.Entity<District>(entity =>
                  {
                        entity.HasKey(d => d.Id);

                        entity.Property(d => d.Name)
                        .IsRequired()
                        .HasMaxLength(100);

                        // Relationship: Province -> Districts (Restrict Delete)
                        entity.HasOne(d => d.Province)
                        .WithMany(p => p.Districts)
                        .HasForeignKey(d => d.ProvinceId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });

                  // 9. Municipality Entity Configuration
                  modelBuilder.Entity<Municipality>(entity =>
                  {
                        entity.HasKey(m => m.Id);

                        entity.Property(m => m.Name)
                        .IsRequired()
                        .HasMaxLength(100);

                        // Relationship: District -> Municipalities (Restrict Delete)
                        entity.HasOne(m => m.District)
                        .WithMany(d => d.Municipalities)
                        .HasForeignKey(m => m.DistrictId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });
            }
      }

      public class DbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
      {
            public AuctionDbContext CreateDbContext(string[] args)
            {
                  IConfigurationRoot configuration = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false)
                      .AddJsonFile("appsettings.Development.json", optional: true)
                      .AddUserSecrets<AuctionDbContext>()
                      .AddEnvironmentVariables()
                      .Build();

                  var optionsBuilder = new DbContextOptionsBuilder<AuctionDbContext>();
                  var connectionString = configuration.GetConnectionString("DefaultConnection");

                  optionsBuilder.UseSqlServer(connectionString);

                  return new AuctionDbContext(optionsBuilder.Options);
            }
      }
}