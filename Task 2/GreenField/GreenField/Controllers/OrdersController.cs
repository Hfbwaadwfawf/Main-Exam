using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using System.Security.Claims;

namespace GreenField.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Orders.Include(o => o.DiscountCode);
            return View(await applicationDbContext.ToListAsync());
        }

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

        [HttpPost]
        public async Task<IActionResult> Checkout(bool IsDelivery, string? DeliveryAddress, DateOnly? CollectionDate, string? DiscountCodeInput)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (IsDelivery && string.IsNullOrWhiteSpace(DeliveryAddress))
                ModelState.AddModelError("DeliveryAddress", "Please enter a delivery address.");

            if (!IsDelivery && (CollectionDate == null || CollectionDate < DateOnly.FromDateTime(DateTime.UtcNow)))
                ModelState.AddModelError("CollectionDate", "Please select a valid collection date.");

            if (!ModelState.IsValid)
            {
                var basket = await _context.Basket
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == true);

                var basketProducts = basket != null
                    ? await _context.BasketProducts
                        .Where(x => x.BasketId == basket.BasketId)
                        .Include(x => x.Products)
                        .ToListAsync()
                    : new List<BasketProducts>();

                decimal subtotal = basketProducts.Sum(x => x.Products.Price * x.Quantity);
                var orderCount = await _context.Orders.CountAsync(x => x.UserId == userId);

                ViewBag.BasketProducts = basketProducts;
                ViewBag.Subtotal = subtotal;
                ViewBag.LoyaltyDiscount = orderCount >= 5 ? subtotal * 0.10m : 0m;
                ViewBag.OrderCount = orderCount;

                return View();
            }

            var basketFull = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == true);

            if (basketFull == null || !basketFull.BasketProducts.Any())
                return RedirectToAction("Index", "Baskets");

            decimal subtotalFinal = basketFull.BasketProducts.Sum(x => x.Products.Price * x.Quantity);

            var orderCountFinal = await _context.Orders.CountAsync(x => x.UserId == userId);
            decimal loyaltyDiscountFinal = orderCountFinal >= 5 ? subtotalFinal * 0.10m : 0m;
            subtotalFinal -= loyaltyDiscountFinal;

            decimal deliveryFee = IsDelivery ? 3.99m : 0m;

            DiscountCodes? discountCode = null;
            decimal codeDiscount = 0m;

            if (!string.IsNullOrWhiteSpace(DiscountCodeInput))
            {
                discountCode = await _context.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == DiscountCodeInput && d.IsActive);

                if (discountCode != null)
                {
                    codeDiscount = subtotalFinal * (discountCode.Percentage / 100m);
                    subtotalFinal -= codeDiscount;
                }
                else
                {
                    ModelState.AddModelError("DiscountCodeInput", "Invalid or inactive discount code.");

                    ViewBag.BasketProducts = basketFull.BasketProducts;
                    ViewBag.Subtotal = basketFull.BasketProducts.Sum(x => x.Products.Price * x.Quantity);
                    ViewBag.LoyaltyDiscount = loyaltyDiscountFinal;
                    ViewBag.OrderCount = orderCountFinal;

                    return View();
                }
            }

            decimal total = subtotalFinal + deliveryFee;

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
                UsedDiscount = discountCode != null || loyaltyDiscountFinal > 0,
                DiscountName = discountCode?.Code,
                DiscountCodeId = discountCode?.DiscountCodesId,
                OrderProducts = basketFull.BasketProducts.Select(bp => new OrderProducts
                {
                    ProductsId = bp.ProductsId,
                    Quantity = bp.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);

            _context.BasketProducts.RemoveRange(basketFull.BasketProducts);
            basketFull.Status = false;

            var loyaltyPoints = await _context.LoyaltyPoints.FirstOrDefaultAsync(u => u.UserId == userId);
            if (loyaltyPoints == null)
            {
                loyaltyPoints = new LoyaltyPoints { UserId = userId, Points = 0 };
                _context.LoyaltyPoints.Add(loyaltyPoints);
            }

            loyaltyPoints.Points += (int)(total * 100);

            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = order.OrdersId });
        }

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

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IsDelivery,DeliveryAddress,DeliveryFee,CollectionDate,DiscountName,DiscountCodeId")] Orders orders)
        {
            if (ModelState.IsValid)
            {
                orders.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                orders.OrderDate = DateTime.UtcNow;
                orders.Status = OrderStatus.Pending;
                orders.TotalPrice = 0;

                _context.Add(orders);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId", orders.DiscountCodeId);
            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var orders = await _context.Orders.FindAsync(id);
            if (orders == null) return NotFound();

            ViewData["DiscountCodeId"] = new SelectList(_context.DiscountCodes, "DiscountCodesId", "DiscountCodesId", orders.DiscountCodeId);
            ViewData["StatusList"] = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>()
                .Select(s => new { Value = (int)s, Text = s.ToString() }), "Value", "Text", (int)orders.Status);

            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrdersId,IsDelivery,DeliveryAddress,DeliveryFee,CollectionDate,DiscountName,DiscountCodeId,Status")] Orders orders)
        {
            if (id != orders.OrdersId) return NotFound();

            var existing = await _context.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    orders.UserId = existing.UserId;
                    orders.OrderDate = existing.OrderDate;
                    orders.TotalPrice = existing.TotalPrice;

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

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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