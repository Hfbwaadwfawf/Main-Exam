using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GreenField.Models;

namespace GreenField.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<GreenField.Models.Basket> Basket { get; set; } = default!;
        public DbSet<GreenField.Models.BasketProducts> BasketProducts { get; set; } = default!;
        public DbSet<GreenField.Models.DiscountCodes> DiscountCodes { get; set; } = default!;
        public DbSet<GreenField.Models.LoyaltyPoints> LoyaltyPoints { get; set; } = default!;
        public DbSet<GreenField.Models.OrderProducts> OrderProducts { get; set; } = default!;
        public DbSet<GreenField.Models.Orders> Orders { get; set; } = default!;
        public DbSet<GreenField.Models.Producers> Producers { get; set; } = default!;
        public DbSet<GreenField.Models.ProducerStamps> ProducerStamps { get; set; } = default!;
        public DbSet<GreenField.Models.Products> Products { get; set; } = default!;
        public DbSet<GreenField.Models.ProductStamps> ProductStamps { get; set; } = default!;
        public DbSet<GreenField.Models.Stamps> Stamps { get; set; } = default!;
    }
}
