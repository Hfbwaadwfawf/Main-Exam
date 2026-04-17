using System.Security.Claims;
using GreenField.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Producer")]
    public class ProducerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProducerDashboardController (ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var Producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == UserId);
            if (Producer == null)
            {
                return NotFound();
            }
            var products = await _context.Products.Where (x => x.ProducersId == Producer.ProducersId).ToListAsync();
            var orders = await _context.Orders.Include(o => o.OrderProducts).ThenInclude(op => op.Products).Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == Producer.ProducersId)).ToListAsync();
            ViewBag.TotalProducts = products.Count;
            ViewBag.LowStockCount = products.Count(x =>x.Stock <= 5);
            ViewBag.RecentOrders = orders;

            return View();
        }
    }
}
