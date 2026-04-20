using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orderProducts = await _context.OrderProducts
                .Include(o => o.Orders)
                .Include(o => o.Products)
                    .ThenInclude(p => p.Producers)
                .ToListAsync();

            return View(orderProducts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var orderProduct = await _context.OrderProducts
                .Include(o => o.Orders)
                .Include(o => o.Products)
                .FirstOrDefaultAsync(m => m.OrderProductsId == id);

            if (orderProduct == null) return NotFound();
            return View(orderProduct);
        }
    }
}