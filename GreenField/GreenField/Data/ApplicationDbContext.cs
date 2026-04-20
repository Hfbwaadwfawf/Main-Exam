using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GreenField.Models;

namespace GreenField.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Basket> Basket { get; set; } = default!;
        public DbSet<Products> Products { get; set; } = default!;
        public DbSet<BasketProducts> BasketProducts { get; set; } = default!;
        public DbSet<DiscountCodes> DiscountCodes { get; set; } = default!;
        public DbSet<LoyaltyPoints> LoyaltyPoints { get; set; } = default!;
        public DbSet<Orders> Orders { get; set; } = default!;
        public DbSet<Producers> Producers { get; set; } = default!;
        public DbSet<OrderProducts> OrderProducts { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Prevent cascade delete conflict on Orders -> DiscountCodes
            builder.Entity<Orders>()
                .HasOne(o => o.DiscountCode)
                .WithMany(d => d.Orders)
                .HasForeignKey(o => o.DiscountCodeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}