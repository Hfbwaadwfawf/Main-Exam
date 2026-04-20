using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin"))
            {
                var allOrders = await _context.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Products)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return View(allOrders);
            }

            if (User.IsInRole("Producer"))
            {
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null) return Forbid();

                var producerOrders = await _context.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Products)
                    .Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == producer.ProducersId))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return View(producerOrders);
            }

            var myOrders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                        .ThenInclude(p => p.Producers)
                .FirstOrDefaultAsync(m => m.OrdersId == id);

            if (order == null) return NotFound();

            if (!User.IsInRole("Admin") && order.UserId != userId)
            {
                if (User.IsInRole("Producer"))
                {
                    var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                    bool hasProduct = order.OrderProducts.Any(op => op.Products.ProducersId == producer?.ProducersId);
                    if (!hasProduct) return Forbid();
                }
                else
                {
                    return Forbid();
                }
            }

            return View(order);
        }

        // POST: Orders/UpdateStatus — Admin and Producer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                var hasProduct = await _context.OrderProducts
                    .AnyAsync(op => op.OrdersId == orderId && op.Products.ProducersId == producer!.ProducersId);
                if (!hasProduct) return Forbid();
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Order status updated.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/Cancel — Customer cancels own pending order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) return NotFound();
            if (order.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Order cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Delete/5 — Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .FirstOrDefaultAsync(m => m.OrdersId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
                _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}