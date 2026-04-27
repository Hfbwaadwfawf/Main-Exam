using GreenField.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenField.Data
{
    /// Provides static seeding methods called from Program.cs on startup.
    /// Each method is idempotent — it checks whether data already exists
    /// before inserting, so re-running on a populated database is safe.
    public class SeedData
    {
        /// Creates the three application roles (Admin, Producer, Standard)
        /// if they don't already exist, then seeds five demo user accounts —
        /// one admin, three producers, and one standard customer.
        public static async Task SeedUsersAndRoles(
            IServiceProvider serviceProvider,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Define the three roles the application uses for authorisation
            string[] roleNames = { "Admin", "Producer", "Standard" };

            // Create each role only if it does not already exist in AspNetRoles
            foreach (string roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Local helper — creates a user if one with that email doesn't exist,
            // then assigns them to the specified role if not already assigned.
            async Task CreateUser(string email, string role)
            {
                var user = await userManager.FindByEmailAsync(email);

                // Only create the account if it isn't already in the database
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        // Pre-confirm the email so the account can log in immediately
                        // without needing an email verification step
                        EmailConfirmed = true
                    };

                    // All seeded accounts share this default password for testing
                    await userManager.CreateAsync(user, "Password123!");
                }

                // Add the user to the role if they aren't already a member
                if (!await userManager.IsInRoleAsync(user, role))
                    await userManager.AddToRoleAsync(user, role);
            }

            // Seed the five demo accounts used throughout development and testing
            await CreateUser("admin@example.com", "Admin");
            await CreateUser("producer@example.com", "Producer");
            await CreateUser("producer2@example.com", "Producer");
            await CreateUser("producer3@example.com", "Producer");
            await CreateUser("user@example.com", "Standard");
        }

        public static async Task SeedProducers(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Guard — don't seed if producers table already has rows
            if (context.Producers.Any())
                return;

            // Resolve the Identity user IDs for the three producer accounts
            var p1 = await userManager.FindByEmailAsync("producer@example.com");
            var p2 = await userManager.FindByEmailAsync("producer2@example.com");
            var p3 = await userManager.FindByEmailAsync("producer3@example.com");

            // All three must exist (created in SeedUsersAndRoles) before we can proceed
            if (p1 == null || p2 == null || p3 == null)
                throw new Exception("Producer user not found.");

            // Build the producer records, linking each to its Identity user
            var producers = new List<Producers>
            {
                new Producers
                {
                    BusinessName        = "Fresh Farm Produce",
                    BusinessDescription = "Local organic farm supplying fresh produce.",
                    BusinessBasedIn     = "Birmingham",
                    Logo                = "/images/DemoLogo.png",
                    UserId              = p1.Id  // Foreign key to AspNetUsers
                },
                new Producers
                {
                    BusinessName        = "UK Big Farm",
                    BusinessDescription = "Large scale UK agricultural supplier.",
                    BusinessBasedIn     = "London",
                    Logo                = "/images/DemoLogo.png",
                    UserId              = p2.Id
                },
                new Producers
                {
                    BusinessName        = "Sandwell College Farm",
                    BusinessDescription = "Student-run sustainable farm.",
                    BusinessBasedIn     = "Sandwell",
                    Logo                = "/images/DemoLogo.png",
                    UserId              = p3.Id
                }
            };

            context.Producers.AddRange(producers);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds 21 products spread across 9 categories (Dairy, Bakery, Eggs,
        /// Preserves, Oils, Drinks, Vegetables, Fruit, Meat), each assigned
        /// to one of the three seeded producers.
        /// Skips if products already exist.
        /// </summary>
        public static async Task SeedProducts(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Resolve the three producer records by business name so we can
            // assign their ProducersId as a foreign key on each product
            var fresh = await context.Producers.FirstOrDefaultAsync(x => x.BusinessName == "Fresh Farm Produce");
            var big = await context.Producers.FirstOrDefaultAsync(x => x.BusinessName == "UK Big Farm");
            var sandwell = await context.Producers.FirstOrDefaultAsync(x => x.BusinessName == "Sandwell College Farm");

            // Producers must be seeded first — throw if any are missing
            if (fresh == null || big == null || sandwell == null)
                throw new Exception("Producer not found.");

            // Guard — don't seed if products table already has rows
            if (context.Products.Any())
                return;

            var products = new List<Products>
            {
                // Dairy 
                new Products { ProductName = "Milk",          Stock = 100, Price = 1.20m, category = "Dairy",     ProducersId = sandwell.ProducersId, Image = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400" },
                new Products { ProductName = "Butter",        Stock = 75,  Price = 1.80m, category = "Dairy",     ProducersId = big.ProducersId,      Image = "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=400" },
                new Products { ProductName = "Cheddar Cheese",Stock = 50,  Price = 3.50m, category = "Dairy",     ProducersId = fresh.ProducersId,    Image = "https://images.unsplash.com/photo-1618164435735-413d3b066c9a?w=400" },
                new Products { ProductName = "Yoghurt",       Stock = 60,  Price = 1.90m, category = "Dairy",     ProducersId = sandwell.ProducersId, Image = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400" },

                //  Bakery 
                new Products { ProductName = "Sourdough Bread", Stock = 40, Price = 2.50m, category = "Bakery",   ProducersId = sandwell.ProducersId, Image = "https://images.unsplash.com/photo-1586444248902-2f64eddc13df?w=400" },

                //  Eggs 
                new Products { ProductName = "Free Range Eggs (6 pack)", Stock = 120, Price = 2.20m, category = "Eggs", ProducersId = sandwell.ProducersId, Image = "https://images.unsplash.com/photo-1506976785307-8732e854ad03?w=400" },

                //  Preserves 
                new Products { ProductName = "Strawberry Jam", Stock = 55, Price = 2.80m, category = "Preserves", ProducersId = fresh.ProducersId, Image = "https://images.unsplash.com/photo-1500912239908-4ee48acb3a7f?w=400" },
                new Products { ProductName = "Marmalade",      Stock = 35, Price = 2.90m, category = "Preserves", ProducersId = big.ProducersId,   Image = "https://images.unsplash.com/photo-1645871317023-00ca188755de?w=400" },

                //  Oils 
                new Products { ProductName = "Rapeseed Oil", Stock = 30, Price = 4.50m, category = "Oils", ProducersId = big.ProducersId, Image = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400" },

                //  Drinks 
                new Products { ProductName = "Apple Cider",   Stock = 40, Price = 3.80m, category = "Drinks", ProducersId = fresh.ProducersId, Image = "https://plus.unsplash.com/premium_photo-1663089590359-6ec775dd518e?w=400" },
                new Products { ProductName = "Orange Juice",  Stock = 80, Price = 1.50m, category = "Drinks", ProducersId = fresh.ProducersId, Image = "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=400" },

                //  Vegetables 
                new Products { ProductName = "Carrots (1kg)",   Stock = 90,  Price = 0.90m, category = "Vegetables", ProducersId = sandwell.ProducersId, Image = "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?w=400" },
                new Products { ProductName = "Potatoes (2kg)",  Stock = 100, Price = 1.50m, category = "Vegetables", ProducersId = big.ProducersId,      Image = "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400" },
                new Products { ProductName = "Onions (1kg)",    Stock = 85,  Price = 0.80m, category = "Vegetables", ProducersId = fresh.ProducersId,    Image = "https://images.unsplash.com/photo-1620574387735-3624d75b2dbc?w=400" },
                new Products { ProductName = "Tomatoes (500g)", Stock = 65,  Price = 1.40m, category = "Vegetables", ProducersId = fresh.ProducersId,    Image = "https://images.unsplash.com/photo-1582284540020-8acbe03f4924?w=400" },
                new Products { ProductName = "Cucumber",        Stock = 60,  Price = 0.70m, category = "Vegetables", ProducersId = big.ProducersId,      Image = "https://images.unsplash.com/photo-1449300079323-02e209d9d3a6?w=400" },
                new Products { ProductName = "Broccoli",        Stock = 55,  Price = 1.10m, category = "Vegetables", ProducersId = big.ProducersId,      Image = "https://images.unsplash.com/photo-1459411621453-7b03977f4bfc?w=400" },

                //  Fruit 
                new Products { ProductName = "Apples (6 pack)", Stock = 70, Price = 1.80m, category = "Fruit", ProducersId = fresh.ProducersId, Image = "https://images.unsplash.com/photo-1567306226416-28f0efdc88ce?w=400" },

                //  Meat 
                new Products { ProductName = "Whole Chicken",        Stock = 20, Price = 8.50m, category = "Meat", ProducersId = big.ProducersId,      Image = "https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=400" },
                new Products { ProductName = "Beef Mince (500g)",    Stock = 30, Price = 5.00m, category = "Meat", ProducersId = big.ProducersId,      Image = "https://plus.unsplash.com/premium_photo-1670357599582-de7232e949a0?w=400" },
                new Products { ProductName = "Pork Sausages (6 pack)",Stock = 35, Price = 4.20m, category = "Meat", ProducersId = sandwell.ProducersId, Image = "https://images.unsplash.com/photo-1691480241974-92481cef09ff?w=400" }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        public static async Task SeedDiscountCodes(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Guard — don't seed if codes already exist
            if (context.DiscountCodes.Any())
                return;

            var codes = new List<DiscountCodes>
            {
                // Standard promotional codes — no points required to use
                new DiscountCodes { Code = "WELCOME10",    Percentage = 10, IsActive = true, PointsRequired = 0 },
                new DiscountCodes { Code = "GREENFIELD20", Percentage = 20, IsActive = true, PointsRequired = 0 },

                // Loyalty reward codes — customers must have earned enough
                // points before these codes become valid at checkout
                new DiscountCodes { Code = "LOYAL30", Percentage = 30, IsActive = true, PointsRequired = 300 },  // Requires 300 pts
                new DiscountCodes { Code = "LOYAL50", Percentage = 50, IsActive = true, PointsRequired = 600 }   // Requires 600 pts
            };

            context.DiscountCodes.AddRange(codes);
            await context.SaveChangesAsync();
        }

        public static async Task SeedBasket(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Resolve the standard test user account
            var user = await userManager.FindByEmailAsync("user@example.com");
            if (user == null)
                throw new Exception("User not found.");

            // Look up the two products we want to pre-add to the basket
            var milk = await context.Products.FirstOrDefaultAsync(x => x.ProductName == "Milk");
            var cider = await context.Products.FirstOrDefaultAsync(x => x.ProductName == "Apple Cider");

            // Guard — don't seed if a basket already exists
            if (context.Basket.Any())
                return;

            // Create the basket and its two child BasketProducts records
            // EF Core will handle inserting the child records automatically
            var basket = new Basket
            {
                UserId = user.Id,
                BasketProducts = new List<BasketProducts>
                {
                    new BasketProducts { Products = milk,  Quantity = 2 },
                    new BasketProducts { Products = cider, Quantity = 1 }
                }
            };

            context.Basket.Add(basket);
            await context.SaveChangesAsync();
        }
    }
}