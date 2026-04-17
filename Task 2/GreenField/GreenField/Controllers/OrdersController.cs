using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using System.Security.Claims;

namespace GreenField.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Orders.Include(o => o.DiscountCode);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var orders = await _context.Orders
                .Include(o => o.DiscountCode)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .FirstOrDefaultAsync(m => m.OrdersId == id);

            if (orders == null) return NotFound();

            return View(orders);
        }

        // GET: Orders/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized();

            var basket = await _context.Basket
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == true);

            if (basket == null) return RedirectToAction("Index", "Baskets");

            var basketProducts = await _context.BasketProducts
                .Where(x => x.BasketId == basket.BasketId)
                .Include(x => x.Products)
                .ToListAsync();

            if (!basketProducts.Any()) return RedirectToAction("Index", "Baskets");

            decimal subtotal = basketProducts.Sum(x => x.Products.Price * x.Quantity);

            var orderCount = await _context.Orders.CountAsync(x => x.UserId == userId);
            decimal loyaltyDiscount = orderCount >= 5 ? subtotal * 0.10m : 0m;

            ViewBag.BasketProducts = basketProducts;
            ViewBag.Subtotal = subtotal;
            ViewBag.LoyaltyDiscount = loyaltyDiscount;
            ViewBag.OrderCount = orderCount;

            return View();
        }

        // POST: Orders/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout(bool IsDelivery, string? DeliveryAddress, DateOnly? CollectionDate, string? DiscountCodeInput)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized();

            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == true);

            if (basket == null || !basket.BasketProducts.Any())
                return RedirectToAction("Index", "Baskets");

            decimal subtotal = basket.BasketProducts.Sum(x => x.Products.Price * x.Quantity);

            // Loyalty discount
            var orderCount = await _context.Orders.CountAsync(x => x.UserId == userId);
            decimal loyaltyDiscount = orderCount >= 5 ? subtotal * 0.10m : 0m;
            subtotal -= loyaltyDiscount;

            // Delivery fee
            decimal deliveryFee = IsDelivery ? 3.99m : 0m;

            // Discount code
            DiscountCodes? discountCode = null;
            decimal codeDiscount = 0m;

            if (!string.IsNullOrWhiteSpace(DiscountCodeInput))
            {
                discountCode = await _context.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == DiscountCodeInput && d.IsActive);

                if (discountCode != null)
                {
                    codeDiscount = subtotal * (discountCode.Percentage / 100m);
                    subtotal -= codeDiscount;
                }
                else
                {
                    ModelState.AddModelError("DiscountCodeInput", "Invalid or inactive discount code.");

                    var basketProducts = await _context.BasketProducts
                        .Where(x => x.BasketId == basket.BasketId)
                        .Include(x => x.Products)
                        .ToListAsync();

                    ViewBag.BasketProducts = basketProducts;
                    ViewBag.Subtotal = basket.BasketProducts.Sum(x => x.Products.Price * x.Quantity);
                    ViewBag.LoyaltyDiscount = loyaltyDiscount;
                    ViewBag.OrderCount = orderCount;

                    return View();
                }
            }

            decimal total = subtotal + deliveryFee;

            // Create order
            var order = new Orders
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalPrice = total,
                Status = OrderStatus.Pending,
                IsDelivery = IsDelivery,
                DeliveryAddress = IsDelivery ? DeliveryAddress : null,
                DeliveryFee = deliveryFee,
                CollectionDate = !IsDelivery ? CollectionDate : null,
                UsedDiscount = discountCode != null || loyaltyDiscount > 0,
                DiscountName = discountCode?.Code,
                DiscountCodeId = discountCode?.DiscountCodesId,
                OrderProducts = basket.BasketProducts.Select(bp => new OrderProducts
                {
                    ProductsId = bp.ProductsId,
                    Quantity = bp.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);

            // Clear basket
            _context.BasketProducts.RemoveRange(basket.BasketProducts);
            basket.Status = false;

            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = order.OrdersId });
        }

        // GET: Orders/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Include(o => o.DiscountCode)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrdersId,UserId,OrderDate,TotalPrice,Status,IsDelivery,DeliveryAddress,DeliveryFee,CollectionDate,UsedDiscount,DiscountName,DiscountCodeId")] Orders orders)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orders);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId", orders.DiscountCodeId);
            return View(orders);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var orders = await _context.Orders.FindAsync(id);
            if (orders == null) return NotFound();

            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId", orders.DiscountCodeId);
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrdersId,UserId,OrderDate,TotalPrice,Status,IsDelivery,DeliveryAddress,DeliveryFee,CollectionDate,UsedDiscount,DiscountName,DiscountCodeId")] Orders orders)
        {
            if (id != orders.OrdersId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orders);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrdersExists(orders.OrdersId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId", orders.DiscountCodeId);
            return View(orders);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var orders = await _context.Orders
                .Include(o => o.DiscountCode)
                .FirstOrDefaultAsync(m => m.OrdersId == id);

            if (orders == null) return NotFound();

            return View(orders);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orders = await _context.Orders.FindAsync(id);
            if (orders != null) _context.Orders.Remove(orders);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrdersExists(int id)
        {
            return _context.Orders.Any(e => e.OrdersId == id);
        }
    }
}