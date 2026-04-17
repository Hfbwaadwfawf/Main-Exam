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

            // Seed admin user
            var adminUser = await userManager.FindByEmailAsync("admin@example.com");
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(adminUser, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");

            // Seed producer user 1
            var producerUser = await userManager.FindByEmailAsync("producer@example.com");
            if (producerUser == null)
            {
                producerUser = new IdentityUser { UserName = "producer@example.com", Email = "producer@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(producerUser, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(producerUser, "Producer"))
                await userManager.AddToRoleAsync(producerUser, "Producer");

            // Seed producer user 2
            var producerUser2 = await userManager.FindByEmailAsync("producer2@example.com");
            if (producerUser2 == null)
            {
                producerUser2 = new IdentityUser { UserName = "producer2@example.com", Email = "producer2@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(producerUser2, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(producerUser2, "Producer"))
                await userManager.AddToRoleAsync(producerUser2, "Producer");

            // Seed producer user 3
            var producerUser3 = await userManager.FindByEmailAsync("producer3@example.com");
            if (producerUser3 == null)
            {
                producerUser3 = new IdentityUser { UserName = "producer3@example.com", Email = "producer3@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(producerUser3, "Password123!");
            }
            if (!await userManager.IsInRoleAsync(producerUser3, "Producer"))
                await userManager.AddToRoleAsync(producerUser3, "Producer");

            // Seed standard user
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

        public static async Task SeedStamps(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.Stamps.Any())
                return;

            var stamps = new List<Stamps>
            {
                new Stamps { StampName = "Organic", StampDescription = "Produced without synthetic pesticides or fertilisers." },
                new Stamps { StampName = "Seasonal", StampDescription = "Only available during its natural growing season." },
                new Stamps { StampName = "Locally Sourced", StampDescription = "Sourced within a short distance of where it is sold." },
                new Stamps { StampName = "Raw & Unfiltered", StampDescription = "Completely unprocessed and in its most natural state." },
                new Stamps { StampName = "Artisan", StampDescription = "Handcrafted in small batches using traditional methods." },
                new Stamps { StampName = "Gluten Free", StampDescription = "Contains no gluten, suitable for coeliacs." },
                new Stamps { StampName = "Soil Association Certified", StampDescription = "Certified organic by the Soil Association." },
                new Stamps { StampName = "Red Tractor Approved", StampDescription = "Meets Red Tractor standards for food safety and traceability." },
                new Stamps { StampName = "Family Run Business", StampDescription = "Independently owned and operated by a family." },
                new Stamps { StampName = "Sustainably Farmed", StampDescription = "Farmed using methods that protect the environment for future generations." },
                new Stamps { StampName = "Award Winning", StampDescription = "Recognised with industry awards for quality and excellence." }
            };

            context.Stamps.AddRange(stamps);
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

            var organic = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Organic");
            var seasonal = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Seasonal");
            var locallySourced = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Locally Sourced");
            var rawAndUnfiltered = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Raw & Unfiltered");
            var artisan = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Artisan");
            var glutenFree = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Gluten Free");
            var soilAssociation = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Soil Association Certified");
            var redTractor = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Red Tractor Approved");
            var familyRun = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Family Run Business");
            var sustainablyFarmed = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Sustainably Farmed");
            var awardWinning = await context.Stamps.FirstOrDefaultAsync(x => x.StampName == "Award Winning");

            if (context.Products.Any())
                return;

            var milk = new Products
            {
                ProductName = "Milk",
                Stock = 100,
                Price = 1.20m,
                ProducersId = SandwellCollegeFarm.ProducersId,
                Image = "/images/milk.png",
                ProductStamps = new List<ProductStamps>
                {
                    new ProductStamps { Stamps = organic },
                    new ProductStamps { Stamps = locallySourced }
                }
            };

            var orangeJuice = new Products
            {
                ProductName = "Orange Juice",
                Stock = 80,
                Price = 1.50m,
                ProducersId = FreshFarmProduce.ProducersId,
                Image = "/images/orangejuice.png",
                ProductStamps = new List<ProductStamps>
                {
                    new ProductStamps { Stamps = organic },
                    new ProductStamps { Stamps = seasonal }
                }
            };

            var honey = new Products
            {
                ProductName = "Honey",
                Stock = 60,
                Price = 3.00m,
                ProducersId = UkBigFarm.ProducersId,
                Image = "/images/honey.png",
                ProductStamps = new List<ProductStamps>
                {
                    new ProductStamps { Stamps = seasonal },
                    new ProductStamps { Stamps = rawAndUnfiltered }
                }
            };

            var sourdough = new Products
            {
                ProductName = "Sourdough",
                Stock = 40,
                Price = 2.50m,
                ProducersId = SandwellCollegeFarm.ProducersId,
                Image = "/images/sourdough.png",
                ProductStamps = new List<ProductStamps>
                {
                    new ProductStamps { Stamps = artisan },
                    new ProductStamps { Stamps = glutenFree }
                }
            };

            await context.Products.AddRangeAsync(new List<Products> { milk, orangeJuice, honey, sourdough });
            await context.SaveChangesAsync();

            // Seed producer stamps
            if (!context.ProducerStamps.Any())
            {
                var producerStamps = new List<ProducerStamps>
                {
                    new ProducerStamps { ProducersId = FreshFarmProduce.ProducersId, Stamps = soilAssociation },
                    new ProducerStamps { ProducersId = FreshFarmProduce.ProducersId, Stamps = familyRun },
                    new ProducerStamps { ProducersId = UkBigFarm.ProducersId, Stamps = redTractor },
                    new ProducerStamps { ProducersId = UkBigFarm.ProducersId, Stamps = awardWinning },
                    new ProducerStamps { ProducersId = SandwellCollegeFarm.ProducersId, Stamps = sustainablyFarmed },
                    new ProducerStamps { ProducersId = SandwellCollegeFarm.ProducersId, Stamps = familyRun }
                };

                context.ProducerStamps.AddRange(producerStamps);
                await context.SaveChangesAsync();
            }
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