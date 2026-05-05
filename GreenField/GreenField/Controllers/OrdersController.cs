using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    // whole controller requires login
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // inject db and user manager
        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET — order list, filtered by role
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin"))
            {
                // admins see all orders
                var allOrders = await _context.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Products)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return View(allOrders);
            }

            if (User.IsInRole("Producer"))
            {
                // producers only see orders containing their products
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

            // standard users only see their own orders
            var myOrders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }

        // GET — order details are shown inline on the dashboard now, so this just redirects there
        public IActionResult Details(int? id)
        {
            TempData["Success"] = "View your order details in your dashboard below.";
            return RedirectToAction("Index", "Dashboard");
        }

        // POST — update order status, admin and producer only
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
                // check the producer actually has a product in this order before allowing status change
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                var hasProduct = await _context.OrderProducts
                    .AnyAsync(op => op.OrdersId == orderId && op.Products.ProducersId == producer!.ProducersId);
                if (!hasProduct) return Forbid();
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Order status updated.";

            // send back to dashboard where the order rows live
            return RedirectToAction("Index", "Dashboard");
        }

        // POST — AJAX cancel from the standard dashboard, returns JSON so the button can update without a reload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAjax(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) return NotFound();

            // only the order owner or an admin can cancel
            if (order.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // can only cancel while still pending
            if (order.Status != OrderStatus.Pending)
                return Json(new { success = false, message = "Only pending orders can be cancelled." });

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST — regular cancel action (non-AJAX), redirects after cancelling
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

        // GET — edit order page, standard users only, pending orders only
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();

            // only the owner can edit their order
            if (order.UserId != userId) return Forbid();

            // can only edit while still pending
            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Only pending orders can be edited.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(order);
        }

        // POST — saves the edited delivery address or collection date
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int orderId, string? deliveryAddress, DateOnly? collectionDate)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) return NotFound();
            if (order.UserId != userId) return Forbid();

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Only pending orders can be edited.";
                return RedirectToAction("Index", "Dashboard");
            }

            // update whichever field applies to this order type
            if (order.IsDelivery)
            {
                if (string.IsNullOrWhiteSpace(deliveryAddress))
                {
                    TempData["Error"] = "Please enter a delivery address.";
                    return View(order);
                }
                order.DeliveryAddress = deliveryAddress;
            }
            else
            {
                if (collectionDate == null || collectionDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    TempData["Error"] = "Please choose a valid future collection date.";
                    return View(order);
                }
                order.CollectionDate = collectionDate;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order updated successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        // GET — delete confirmation page, admin only
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

        // POST — deletes an order, admin only
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
