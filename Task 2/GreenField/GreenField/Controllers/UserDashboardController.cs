using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using System.Security.Claims;

namespace GreenField.Controllers
{
    [Authorize] // All users must be logged in to access their dashboard
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UserDashboard — shows the user's orders, loyalty points, and discount status
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized();

            // Load all orders for this user, most recent first, with product details
            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Fetch loyalty points record — default to 0 if none exists yet
            var loyaltyPoints = await _context.LoyaltyPoints.FirstOrDefaultAsync(u => u.UserId == userId);
            int points = loyaltyPoints?.Points ?? 0;

            int orderCount = orders.Count;
            bool loyaltyUnlocked = orderCount >= 5; // 10% discount unlocks at 5 orders
            int ordersUntilLoyalty = loyaltyUnlocked ? 0 : 5 - orderCount;

            // Pass all stats to the view
            ViewBag.Points = points;
            ViewBag.OrderCount = orderCount;
            ViewBag.LoyaltyUnlocked = loyaltyUnlocked;
            ViewBag.OrdersUntilLoyalty = ordersUntilLoyalty;

            return View(orders);
        }
    }
}