using GreenField.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GreenField.Data
{
    public class SeedData
    {
        public static async Task SeedUsersAndRoles(IServiceProvider serviceProvider, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Producer", "Standard" };
            foreach (string roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminUser = await userManager.FindByEmailAsync("admin@example.com");
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(adminUser, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");

            var producerUser = await userManager.FindByEmailAsync("producer@example.com");
            if (producerUser == null)
            {
                producerUser = new IdentityUser { UserName = "producer@example.com", Email = "producer@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(producerUser, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(producerUser, "Producer"))
                await userManager.AddToRoleAsync(producerUser, "Producer");

            var producerUser2 = await userManager.FindByEmailAsync("producer2@example.com");
            if (producerUser2 == null)
            {
                producerUser2 = new IdentityUser { UserName = "producer2@example.com", Email = "producer2@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(producerUser2, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(producerUser2, "Producer"))
                await userManager.AddToRoleAsync(producerUser2, "Producer");

            var producerUser3 = await userManager.FindByEmailAsync("producer3@example.com");
            if (producerUser3 == null)
            {
                producerUser3 = new IdentityUser { UserName = "producer3@example.com", Email = "producer3@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(producerUser3, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(producerUser3, "Producer"))
                await userManager.AddToRoleAsync(producerUser3, "Producer");

            var normalUser = await userManager.FindByEmailAsync("user@example.com");
            if (normalUser == null)
            {
                normalUser = new IdentityUser { UserName = "user@example.com", Email = "user@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(normalUser, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(normalUser, "Standard"))
                await userManager.AddToRoleAsync(normalUser, "Standard");
        }

        public static async Task SeedProducers(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var ProducerUser1 = await userManager.FindByEmailAsync("producer@example.com");
            var ProducerUser2 = await userManager.FindByEmailAsync("producer2@example.com");
            var ProducerUser3 = await userManager.FindByEmailAsync("producer3@example.com");

            if (ProducerUser1 == null || ProducerUser2 == null || ProducerUser3 == null)
                throw new Exception("Producer user not found.");

            if (context.Producers.Any())
                return;

            var producers = new List<Producers>
            {
                new Producers
                {
                    BusinessName = "Fresh Farm Produce",
                    BusinessDescription = "Local farm supplying organic fruits and vegetables.",
                    BusinessBasedIn = "Birmingham",
                    Logo = "/images/DemoLogo.png",
                    UserId = ProducerUser1.Id
                },
                new Producers
                {
                    BusinessName = "UK Big Farm",
                    BusinessDescription = "Biggest UK farm supplying organic fruits and vegetables.",
                    BusinessBasedIn = "London",
                    Logo = "/images/DemoLogo.png",
                    UserId = ProducerUser2.Id
                },
                new Producers
                {
                    BusinessName = "Sandwell College Farm",
                    BusinessDescription = "Local farm located in Sandwell supplying organic fruits and vegetables.",
                    BusinessBasedIn = "Sandwell",
                    Logo = "/images/DemoLogo.png",
                    UserId = ProducerUser3.Id
                }
            };

            context.Producers.AddRange(producers);
            await context.SaveChangesAsync();
        }

        public static async Task SeedProducts(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var FreshFarmProduce = await context.Producers.FirstOrDefaultAsync(x => x.BusinessName == "Fresh Farm Produce");
            var UkBigFarm = await context.Producers.FirstOrDefaultAsync(x => x.BusinessName == "UK Big Farm");
            var SandwellCollegeFarm = await context.Producers.FirstOrDefaultAsync(x => x.BusinessName == "Sandwell College Farm");

            if (FreshFarmProduce == null || UkBigFarm == null || SandwellCollegeFarm == null)
                throw new Exception("Producer not found.");

            if (context.Products.Any())
                return;

            var products = new List<Products>
            {
                // Dairy & Basics
                new Products { ProductName = "Milk", Stock = 100, Price = 1.20m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400" },
                new Products { ProductName = "Orange Juice", Stock = 80, Price = 1.50m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=400" },
                new Products { ProductName = "Honey", Stock = 60, Price = 3.00m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400" },
                new Products { ProductName = "Sourdough", Stock = 40, Price = 2.50m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1586444248902-2f64eddc13df?w=400" },
                new Products { ProductName = "Free Range Eggs (6 pack)", Stock = 120, Price = 2.20m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1506976785307-8732e854ad03?w=400" },
                new Products { ProductName = "Butter", Stock = 75, Price = 1.80m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=400" },
                new Products { ProductName = "Cheddar Cheese", Stock = 50, Price = 3.50m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1618164435735-413d3b066c9a?w=400" },
                new Products { ProductName = "Natural Yoghurt", Stock = 60, Price = 1.90m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400" },
                new Products { ProductName = "Double Cream", Stock = 45, Price = 1.60m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1587854692152-cbe660dbde88?w=400" },
                new Products { ProductName = "Free Range Duck Eggs (4 pack)", Stock = 30, Price = 3.20m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1569288052389-dac9b0ac9eac?w=400" },

                // Preserves & Oils
                new Products { ProductName = "Strawberry Jam", Stock = 55, Price = 2.80m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1597528380286-cb2adb03de47?w=400" },
                new Products { ProductName = "Blackcurrant Jam", Stock = 40, Price = 2.80m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?w=400" },
                new Products { ProductName = "Marmalade", Stock = 35, Price = 2.90m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1584744982491-665216d95f8b?w=400" },
                new Products { ProductName = "Rapeseed Oil", Stock = 30, Price = 4.50m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1620574387735-3624d75b2dbc?w=400" },

                // Drinks
                new Products { ProductName = "Apple Cider", Stock = 40, Price = 3.80m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400" },
                new Products { ProductName = "Pear Juice", Stock = 35, Price = 2.20m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1546173159-315724a31696?w=400" },

                // Vegetables
                new Products { ProductName = "Carrots (1kg)", Stock = 90, Price = 0.90m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?w=400" },
                new Products { ProductName = "Potatoes (2kg)", Stock = 100, Price = 1.50m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400" },
                new Products { ProductName = "Onions (1kg)", Stock = 85, Price = 0.80m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1587735243615-c03f25aaff15?w=400" },
                new Products { ProductName = "Garlic (3 bulbs)", Stock = 70, Price = 1.20m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1540148426945-6cf22a6b2383?w=400" },
                new Products { ProductName = "Tomatoes (500g)", Stock = 65, Price = 1.40m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1582284540020-8acbe03f4924?w=400" },
                new Products { ProductName = "Cucumber", Stock = 60, Price = 0.70m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1449300079323-02e209d9d3a6?w=400" },
                new Products { ProductName = "Courgette", Stock = 55, Price = 0.90m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1596363505729-4190a9506133?w=400" },
                new Products { ProductName = "Spinach (200g)", Stock = 50, Price = 1.30m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400" },
                new Products { ProductName = "Kale (200g)", Stock = 45, Price = 1.20m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1524179091875-bf99a9a6af57?w=400" },
                new Products { ProductName = "Broccoli", Stock = 55, Price = 1.10m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1459411621453-7b03977f4bfc?w=400" },
                new Products { ProductName = "Leeks (2 pack)", Stock = 40, Price = 1.00m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1587411768638-ec71f8e33b78?w=400" },
                new Products { ProductName = "Beetroot (500g)", Stock = 35, Price = 1.30m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1593105544559-ecb03bf76f82?w=400" },
                new Products { ProductName = "Pumpkin", Stock = 25, Price = 2.50m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1570586437263-ab629fccc818?w=400" },

                // Fruit
                new Products { ProductName = "Apples (6 pack)", Stock = 70, Price = 1.80m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1567306226416-28f0efdc88ce?w=400" },

                // Meat
                new Products { ProductName = "Free Range Whole Chicken", Stock = 20, Price = 8.50m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=400" },
                new Products { ProductName = "Pork Sausages (6 pack)", Stock = 35, Price = 4.20m, ProducersId = SandwellCollegeFarm.ProducersId, Image = "https://images.unsplash.com/photo-1625938144755-652e08e359b7?w=400" },
                new Products { ProductName = "Beef Mince (500g)", Stock = 30, Price = 5.00m, ProducersId = UkBigFarm.ProducersId, Image = "https://images.unsplash.com/photo-1602470520998-f4a52199a3d6?w=400" },
                new Products { ProductName = "Lamb Chops (4 pack)", Stock = 20, Price = 7.50m, ProducersId = FreshFarmProduce.ProducersId, Image = "https://images.unsplash.com/photo-1599021456807-25db0f974333?w=400" },
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        public static async Task SeedDiscountCodes(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.DiscountCodes.Any())
                return;

            var discountCodes = new List<DiscountCodes>
            {
                new DiscountCodes { Code = "WELCOME10", Percentage = 10, IsActive = true },
                new DiscountCodes { Code = "GREENFIELD20", Percentage = 20, IsActive = true },
                new DiscountCodes { Code = "SUMMER15", Percentage = 15, IsActive = true }
            };

            context.DiscountCodes.AddRange(discountCodes);
            await context.SaveChangesAsync();
        }

        public static async Task SeedBasket(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var normalUser = await userManager.FindByEmailAsync("user@example.com");
            if (normalUser == null)
                throw new Exception("Standard user not found.");

            var milk = await context.Products.FirstOrDefaultAsync(x => x.ProductName == "Milk");
            var honey = await context.Products.FirstOrDefaultAsync(x => x.ProductName == "Honey");

            if (milk == null || honey == null)
                throw new Exception("Products not found.");

            if (context.Basket.Any())
                return;

            var basket = new Basket
            {
                Status = true,
                UserId = normalUser.Id,
                BasketProducts = new List<BasketProducts>
                {
                    new BasketProducts { Products = milk, Quantity = 2 },
                    new BasketProducts { Products = honey, Quantity = 1 }
                }
            };

            context.Basket.Add(basket);
            await context.SaveChangesAsync();
        }
    }
}