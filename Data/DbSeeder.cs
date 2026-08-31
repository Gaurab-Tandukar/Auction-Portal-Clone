using Auction_Portal_Clone.Models;
using Microsoft.EntityFrameworkCore;

namespace Auction_Portal_Clone.Data
{
    public static class DbSeeder
    {
        public static async Task SeedInitialDataAsync(AuctionDbContext context)
        {
            // 1. Seed Categories if empty
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Real Estate", Active = true },
                    new Category { Name = "Vehicles", Active = true },
                    new Category { Name = "Industrial Machinery", Active = true },
                    new Category { Name = "Commercial Assets", Active = true },
                    new Category { Name = "Gold & Ornaments", Active = true }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 2. Seed Provinces, Districts, and Municipalities if empty
            if (!await context.Provinces.AnyAsync())
            {
                var bagmati = new Province
                {
                    Name = "Bagmati Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Kathmandu",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Kathmandu Metropolitan City" },
                                new Municipality { Name = "Budhanilkantha Municipality" },
                                new Municipality { Name = "Tarakeshwar Municipality" },
                                new Municipality { Name = "Kirtipur Municipality" },
                                new Municipality { Name = "Tokha Municipality" },
                                new Municipality { Name = "Chandragiri Municipality" }
                            }
                        },
                        new District
                        {
                            Name = "Lalitpur",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Lalitpur Metropolitan City" },
                                new Municipality { Name = "Mahalaxmi Municipality" },
                                new Municipality { Name = "Godawari Municipality" }
                            }
                        },
                        new District
                        {
                            Name = "Bhaktapur",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Bhaktapur Municipality" },
                                new Municipality { Name = "Madhyapur Thimi Municipality" },
                                new Municipality { Name = "Suryabinayak Municipality" },
                                new Municipality { Name = "Changunarayan Municipality" }
                            }
                        },
                        new District
                        {
                            Name = "Chitwan",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Bharatpur Metropolitan City" },
                                new Municipality { Name = "Ratnanagar Municipality" },
                                new Municipality { Name = "Khairahani Municipality" }
                            }
                        }
                    }
                };

                var gandaki = new Province
                {
                    Name = "Gandaki Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Kaski",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Pokhara Metropolitan City" },
                                new Municipality { Name = "Annapurna Rural Municipality" }
                            }
                        }
                    }
                };

                var koshi = new Province
                {
                    Name = "Koshi Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Morang",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Biratnagar Metropolitan City" },
                                new Municipality { Name = "Belbari Municipality" },
                                new Municipality { Name = "Sundarharaicha Municipality" }
                            }
                        },
                        new District
                        {
                            Name = "Sunsari",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Dharan Sub-Metropolitan City" },
                                new Municipality { Name = "Itahari Sub-Metropolitan City" }
                            }
                        }
                    }
                };

                var lumbini = new Province
                {
                    Name = "Lumbini Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Rupandehi",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Butwal Sub-Metropolitan City" },
                                new Municipality { Name = "Siddharthanagar Municipality" },
                                new Municipality { Name = "Tilottama Municipality" }
                            }
                        }
                    }
                };

                var madhesh = new Province
                {
                    Name = "Madhesh Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Parsa",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Birgunj Metropolitan City" },
                                new Municipality { Name = "Pokhariya Municipality" }
                            }
                        }
                    }
                };

                var karnali = new Province
                {
                    Name = "Karnali Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Surkhet",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Birendranagar Municipality" }
                            }
                        }
                    }
                };

                var sudurpashchim = new Province
                {
                    Name = "Sudurpashchim Province",
                    Districts = new List<District>
                    {
                        new District
                        {
                            Name = "Kailali",
                            Municipalities = new List<Municipality>
                            {
                                new Municipality { Name = "Dhangadhi Sub-Metropolitan City" },
                                new Municipality { Name = "Tikapur Municipality" }
                            }
                        }
                    }
                };

                await context.Provinces.AddRangeAsync(bagmati, gandaki, koshi, lumbini, madhesh, karnali, sudurpashchim);
                await context.SaveChangesAsync();
            }
        }
    }
}
