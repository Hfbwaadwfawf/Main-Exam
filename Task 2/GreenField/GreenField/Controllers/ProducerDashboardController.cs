using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using System.Security.Claims;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Producer")] // Producer dashboard — producers only
    public class ProducerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProducerDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProducerDashboard — shows this producer's products and related orders
        public async Task<IActionResult> Index()
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the producer record linked to the logged-in user
            var Producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == UserId);
            if (Producer == null)
            {
                return NotFound();
            }

            // Load all products belonging to this producer
            var products = await _context.Products
                .Where(x => x.ProducersId == Producer.ProducersId)
                .ToListAsync();

            // Load orders that contain at least one product from this producer
            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == Producer.ProducersId))
                .ToListAsync();

            // Pass summary stats to the view
            ViewBag.TotalProducts = products.Count;
            ViewBag.LowStockCount = products.Count(x => x.Stock <= 5);
            ViewBag.RecentOrders = orders;

            return View();
        }
    }
}