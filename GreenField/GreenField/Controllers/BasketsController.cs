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
    public class BasketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // inject db and user manager
        public BasketsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // helper — counts total items in the basket and puts it in ViewData for the nav badge
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

        // GET — shows the user's basket, creates one if they don't have one yet
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // load basket with all products and their producers
            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                        .ThenInclude(p => p.Producers)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            // create a fresh basket if they don't have one
            if (basket == null)
            {
                basket = new Basket { UserId = userId };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            await SetBasketCount();
            return View(basket);
        }

        // GET — called via AJAX on the basket page to validate a discount code, returns JSON
        [HttpGet]
        public async Task<IActionResult> ValidateDiscount(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { valid = false, message = "Enter a code." });

            // check the code exists and is active
            var discount = await _context.DiscountCodes
                .FirstOrDefaultAsync(d => d.Code == code.Trim().ToUpper() && d.IsActive);

            if (discount == null)
                return Json(new { valid = false, message = "Invalid or inactive code." });

            return Json(new { valid = true, percentage = discount.Percentage, code = discount.Code });
        }

        // POST — adds a product to the basket, used for non-JS fallback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            // make sure the product exists, is available and has stock
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsAvailable || product.Stock <= 0)
            {
                TempData["Error"] = "This product is unavailable.";
                return RedirectToAction("Index", "Products");
            }

            // get or create the user's basket
            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (basket == null)
            {
                basket = new Basket { UserId = userId };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            // if the product is already in the basket, increase quantity instead of adding a duplicate
            var existing = basket.BasketProducts?.FirstOrDefault(bp => bp.ProductsId == productId);
            if (existing != null)
                existing.Quantity += quantity;
            else
                _context.BasketProducts.Add(new BasketProducts
                {
                    BasketId = basket.BasketId,
                    ProductsId = productId,
                    Quantity = quantity
                });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Item added to basket.";
            return RedirectToAction("Index", "Products");
        }

        // POST — AJAX version of AddProduct, returns JSON with updated basket count for the nav badge
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductAjax(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsAvailable || product.Stock <= 0)
                return Json(new { success = false, message = "Product unavailable." });

            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (basket == null)
            {
                basket = new Basket { UserId = userId };
                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            // increment quantity if already in basket, otherwise add new line
            var existing = basket.BasketProducts?.FirstOrDefault(bp => bp.ProductsId == productId);
            if (existing != null)
                existing.Quantity += quantity;
            else
                _context.BasketProducts.Add(new BasketProducts
                {
                    BasketId = basket.BasketId,
                    ProductsId = productId,
                    Quantity = quantity
                });

            await _context.SaveChangesAsync();

            // re-count the basket so the nav badge can update
            var basketCount = await _context.BasketProducts
                .Include(bp => bp.Basket)
                .Where(bp => bp.Basket.UserId == userId)
                .SumAsync(bp => bp.Quantity);

            return Json(new { success = true, basketCount });
        }

        // POST — called via AJAX when user changes quantity on the basket page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int basketProductId, int quantity)
        {
            var userId = _userManager.GetUserId(User);

            // make sure this basket item belongs to the current user
            var basketProduct = await _context.BasketProducts
                .Include(bp => bp.Basket)
                .FirstOrDefaultAsync(bp => bp.BasketProductsId == basketProductId && bp.Basket.UserId == userId);

            if (basketProduct == null) return NotFound();

            // if quantity drops to 0 or below, just remove the item
            if (quantity <= 0)
                _context.BasketProducts.Remove(basketProduct);
            else
                basketProduct.Quantity = quantity;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST — removes a single item from the basket, called via AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int basketProductId)
        {
            var userId = _userManager.GetUserId(User);

            // verify ownership before removing
            var basketProduct = await _context.BasketProducts
                .Include(bp => bp.Basket)
                .FirstOrDefaultAsync(bp => bp.BasketProductsId == basketProductId && bp.Basket.UserId == userId);

            if (basketProduct == null) return NotFound();

            _context.BasketProducts.Remove(basketProduct);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET — checkout confirmation page, shows selected items and calculates totals before the user confirms
        [HttpGet]
        public async Task<IActionResult> Confirmation(List<int> selectedProductIds, string? discountCode)
        {
            var userId = _userManager.GetUserId(User);

            // make sure they actually selected something
            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                TempData["Error"] = "Please select at least one item to checkout.";
                return RedirectToAction(nameof(Index));
            }

            // load the basket with full product and producer details
            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                        .ThenInclude(p => p.Producers)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            if (basket == null) return NotFound();

            // filter to only the items the user ticked
            var selectedItems = basket.BasketProducts
                .Where(bp => selectedProductIds.Contains(bp.BasketProductsId))
                .ToList();

            // check each selected item is still available and has enough stock
            foreach (var item in selectedItems)
            {
                if (item.Products == null || !item.Products.IsAvailable || item.Products.Stock < item.Quantity)
                {
                    TempData["Error"] = $"{item.Products?.ProductName ?? "A product"} is out of stock or unavailable.";
                    return RedirectToAction(nameof(Index));
                }
            }

            decimal subtotal = selectedItems.Sum(i => i.Products!.Price * i.Quantity);
            decimal discountPercent = 0;
            string? resolvedCode = null;

            // look up the discount code if one was entered
            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                var dc = await _context.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == discountCode.Trim().ToUpper() && d.IsActive);
                if (dc != null)
                {
                    discountPercent = dc.Percentage;
                    resolvedCode = dc.Code;
                }
            }

            // pass data to the view via ViewBag
            ViewBag.SelectedProductIds = selectedProductIds;
            ViewBag.DiscountCode       = resolvedCode ?? "";
            ViewBag.Subtotal           = subtotal;
            ViewBag.DiscountPercent    = discountPercent;

            return View(selectedItems);
        }

        // POST — actually places the order after the user confirms on the checkout page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            List<int> selectedProductIds,
            bool isDelivery,
            string? deliveryAddress,
            string? discountCode,
            string? discountCodeOverride,   // user can override the code on the confirmation page
            DateOnly? collectionDate)
        {
            var userId = _userManager.GetUserId(User);

            // use the override code if provided, otherwise use the one passed from the basket
            var finalCode = !string.IsNullOrWhiteSpace(discountCodeOverride)
                ? discountCodeOverride
                : discountCode;

            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                TempData["Error"] = "Please select at least one item to checkout.";
                return RedirectToAction(nameof(Index));
            }

            // delivery address is required if they chose delivery
            if (isDelivery && string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["Error"] = "Please enter a delivery address.";
                return RedirectToAction(nameof(Index));
            }

            // collection date must be in the future if they chose click & collect
            if (!isDelivery && (collectionDate == null || collectionDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)))
            {
                TempData["Error"] = "Please choose a valid future collection date.";
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

            // final stock check before committing the order
            foreach (var item in selectedItems)
            {
                if (item.Products == null || !item.Products.IsAvailable || item.Products.Stock < item.Quantity)
                {
                    TempData["Error"] = $"{item.Products?.ProductName ?? "A product"} is out of stock or unavailable.";
                    return RedirectToAction(nameof(Index));
                }
            }

            decimal subtotal    = selectedItems.Sum(i => i.Products!.Price * i.Quantity);
            decimal deliveryFee = isDelivery ? 3.99m : 0m;
            decimal total       = subtotal + deliveryFee;

            DiscountCodes? appliedCode = null;
            bool usedDiscount = false;
            string? discountName = null;

            // look up and apply the discount code if one was provided
            if (!string.IsNullOrWhiteSpace(finalCode))
            {
                appliedCode = await _context.DiscountCodes
                    .FirstOrDefaultAsync(d => d.Code == finalCode.Trim().ToUpper() && d.IsActive);

                if (appliedCode != null)
                {
                    // deduct the discount percentage from the total
                    total -= total * (appliedCode.Percentage / 100);
                    usedDiscount = true;
                    discountName = appliedCode.Code;
                }
            }

            // create the order record
            var order = new Orders
            {
                UserId          = userId!,
                OrderDate       = DateTime.UtcNow,
                TotalPrice      = Math.Round(total, 2),
                IsDelivery      = isDelivery,
                DeliveryAddress = isDelivery ? deliveryAddress : null,
                DeliveryFee     = deliveryFee,
                CollectionDate  = !isDelivery ? collectionDate : null,
                Status          = OrderStatus.Pending,
                UsedDiscount    = usedDiscount,
                DiscountName    = discountName,
                DiscountCodeId  = appliedCode?.DiscountCodesId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // add each selected item as an order product and deduct from stock
            foreach (var item in selectedItems)
            {
                _context.OrderProducts.Add(new OrderProducts
                {
                    OrdersId   = order.OrdersId,
                    ProductsId = item.ProductsId,
                    Quantity   = item.Quantity
                });

                item.Products!.Stock -= item.Quantity;

                // mark unavailable if stock hits zero
                if (item.Products.Stock <= 0)
                    item.Products.IsAvailable = false;
            }

            // remove the ordered items from the basket
            _context.BasketProducts.RemoveRange(selectedItems);
            await _context.SaveChangesAsync();

            // award loyalty points — 10 points per £1 of subtotal (before delivery/discount)
            var loyaltyRecord = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            int pointsEarned = (int)(subtotal * 10);

            if (loyaltyRecord != null)
                loyaltyRecord.Points += pointsEarned;
            else
                _context.LoyaltyPoints.Add(new LoyaltyPoints { UserId = userId!, Points = pointsEarned });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order placed! You earned {pointsEarned} loyalty points.";
            return RedirectToAction(nameof(OrderSuccess), new { orderId = order.OrdersId });
        }

        // GET — order success page shown after placing an order
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            // load the order with its products — only show it to the user who placed it
            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .FirstOrDefaultAsync(o => o.OrdersId == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}
