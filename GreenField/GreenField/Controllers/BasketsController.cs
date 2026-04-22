using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize]
    public class BasketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BasketsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task SetBasketCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                var count = await _context.BasketProducts
                    .Include(bp => bp.Basket)
                    .Where(bp => bp.Basket.UserId == userId)
                    .SumAsync(bp => bp.Quantity);
                ViewData["BasketCount"] = count;
            }
        }

        // GET: Baskets
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                        .ThenInclude(p => p.Producers)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (basket == null)
            {
                basket = new Basket { UserId = userId };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            await SetBasketCount();
            return View(basket);
        }

        // GET: Baskets/ValidateDiscount?code=XXX
        [HttpGet]
        public async Task<IActionResult> ValidateDiscount(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { valid = false, message = "Enter a code." });

            var discount = await _context.DiscountCodes
                .FirstOrDefaultAsync(d => d.Code == code.Trim().ToUpper() && d.IsActive);

            if (discount == null)
                return Json(new { valid = false, message = "Invalid or inactive code." });

            return Json(new { valid = true, percentage = discount.Percentage, code = discount.Code });
        }

        // POST: Baskets/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsAvailable || product.Stock <= 0)
            {
                TempData["Error"] = "This product is unavailable.";
                return RedirectToAction("Index", "Products");
            }

            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (basket == null)
            {
                basket = new Basket { UserId = userId };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            var existing = basket.BasketProducts?
                .FirstOrDefault(bp => bp.ProductsId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _context.BasketProducts.Add(new BasketProducts
                {
                    BasketId = basket.BasketId,
                    ProductsId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Item added to basket.";
            return RedirectToAction("Index", "Products");
        }

        // POST: Baskets/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int basketProductId, int quantity)
        {
            var userId = _userManager.GetUserId(User);

            var basketProduct = await _context.BasketProducts
                .Include(bp => bp.Basket)
                .FirstOrDefaultAsync(bp => bp.BasketProductsId == basketProductId && bp.Basket.UserId == userId);

            if (basketProduct == null) return NotFound();

            if (quantity <= 0)
                _context.BasketProducts.Remove(basketProduct);
            else
                basketProduct.Quantity = quantity;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: Baskets/RemoveProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int basketProductId)
        {
            var userId = _userManager.GetUserId(User);

            var basketProduct = await _context.BasketProducts
                .Include(bp => bp.Basket)
                .FirstOrDefaultAsync(bp => bp.BasketProductsId == basketProductId && bp.Basket.UserId == userId);

            if (basketProduct == null) return NotFound();

            _context.BasketProducts.Remove(basketProduct);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: Baskets/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            List<int> selectedProductIds,
            bool isDelivery,
            string? deliveryAddress,
            string? discountCode)
        {
            var userId = _userManager.GetUserId(User);

            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                TempData["Error"] = "Please select at least one item to checkout.";
                return RedirectToAction(nameof(Index));
            }

            if (isDelivery && string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["Error"] = "Please enter a delivery address.";
                return RedirectToAction(nameof(Index));
            }

            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (basket == null) return NotFound();

            var selectedItems = basket.BasketProducts
                .Where(bp => selectedProductIds.Contains(bp.BasketProductsId))
                .ToList();

            foreach (var item in selectedItems)
            {
                if (item.Products == null || !item.Products.IsAvailable || item.Products.Stock < item.Quantity)
                {
                    TempData["Error"] = $"{item.Products?.ProductName ?? "A product"} is out of stock or unavailable.";
                    return RedirectToAction(nameof(Index));
                }
            }

            decimal subtotal = selectedItems.Sum(i => i.Products!.Price * i.Quantity);
            decimal deliveryFee = isDelivery ? 3.99m : 0m;
            decimal total = subtotal + deliveryFee;

            DiscountCodes? appliedCode = null;
            bool usedDiscount = false;
            string? discountName = null;

            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                appliedCode = await _context.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == discountCode && d.IsActive);

                if (appliedCode == null)
                {
                    TempData["Error"] = "Invalid or inactive discount code.";
                    return RedirectToAction(nameof(Index));
                }

                total -= total * ((decimal)appliedCode.Percentage / 100);
                usedDiscount = true;
                discountName = appliedCode.Code;
            }

            var order = new Orders
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalPrice = Math.Round(total, 2),
                IsDelivery = isDelivery,
                DeliveryAddress = isDelivery ? deliveryAddress : null,
                DeliveryFee = deliveryFee,
                Status = OrderStatus.Pending,
                UsedDiscount = usedDiscount,
                DiscountName = discountName,
                DiscountCodeId = appliedCode?.DiscountCodesId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in selectedItems)
            {
                _context.OrderProducts.Add(new OrderProducts
                {
                    OrdersId = order.OrdersId,
                    ProductsId = item.ProductsId,
                    Quantity = item.Quantity
                });

                item.Products!.Stock -= item.Quantity;
                if (item.Products.Stock <= 0)
                    item.Products.IsAvailable = false;
            }

            _context.BasketProducts.RemoveRange(selectedItems);
            await _context.SaveChangesAsync();

            var loyaltyRecord = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            int pointsEarned = (int)(subtotal * 10);

            if (loyaltyRecord != null)
            {
                loyaltyRecord.Points += pointsEarned;
            }
            else
            {
                _context.LoyaltyPoints.Add(new LoyaltyPoints
                {
                    UserId = userId,
                    Points = pointsEarned
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order placed! You earned {pointsEarned} points.";
            return RedirectToAction("Index", "Orders");
        }
    }
}