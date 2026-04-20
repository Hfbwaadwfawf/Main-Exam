using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using System.Security.Claims;

namespace GreenField.Controllers
{
    [Authorize] // All basket actions require login
    [Route("basket")]
    public class BasketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BasketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /basket — loads the active basket for the logged-in user
        // Creates a new basket if one doesn't exist
        // Calculates subtotal, loyalty discount (10% after 5 orders), and total
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var basket = await _context.Basket
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status);

            // Create a new active basket if none exists
            if (basket == null)
            {
                basket = new Basket
                {
                    Status = true,
                    UserId = userId,
                    BasketCreatedAt = DateTime.UtcNow
                };

                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            // Load basket items with their associated product data
            var basketProducts = await _context.BasketProducts
                .Where(x => x.BasketId == basket.BasketId)
                .Include(x => x.Basket)
                .Include(x => x.Products)
                .ToListAsync();

            // Calculate subtotal from all basket items
            decimal subtotal = 0m;

            foreach (var basketProduct in basketProducts)
            {
                var productTotal = basketProduct.Products.Price * basketProduct.Quantity;
                subtotal += productTotal;
            }

            // Check how many orders the user has placed for loyalty discount eligibility
            var orderCount = await _context.Orders.CountAsync(x => x.UserId == userId);

            decimal discount = 0m;

            // Apply 10% loyalty discount if user has 5 or more completed orders
            if (orderCount >= 5)
            {
                discount = subtotal * 0.10m;
            }

            decimal total = subtotal - discount;

            // Pass totals to the view via ViewBag
            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.Total = total;
            ViewBag.OrderCount = orderCount;

            return View(basketProducts);
        }

        // POST: /basket/create — adds a product to the basket
        // If the product already exists in the basket, increase the quantity
        // Creates a new basket if the user doesn't have an active one
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(int ProductsId)
        {
            // Verify the product exists before adding
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductsId == ProductsId);

            if (product == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Find or create an active basket for this user
            var basket = await _context.Basket.FirstOrDefaultAsync(x => x.UserId == userId && x.Status == true);

            if (basket == null)
            {
                basket = new Basket
                {
                    Status = true,
                    UserId = userId,
                    BasketCreatedAt = DateTime.UtcNow
                };

                _context.Basket.Add(basket);
                await _context.SaveChangesAsync();
            }

            // Check if this product is already in the basket
            var basketProduct = await _context.BasketProducts
                .FirstOrDefaultAsync(bp => bp.BasketId == basket.BasketId && bp.ProductsId == ProductsId);

            if (basketProduct != null)
            {
                // Product already in basket — increment quantity
                basketProduct.Quantity++;
            }
            else
            {
                // Product not in basket — add new basket product entry
                basketProduct = new BasketProducts
                {
                    BasketId = basket.BasketId,
                    ProductsId = ProductsId,
                    Quantity = 1
                };

                _context.BasketProducts.Add(basketProduct);
            }

            await _context.SaveChangesAsync();

            // Return Ok so the AJAX call on the products page doesn't redirect
            return Ok();
        }

        // POST: /basket/remove — removes a basket item entirely by its ID
        [HttpPost]
        [Route("remove")]
        public async Task<IActionResult> Remove(int BasketProductsId)
        {
            var item = await _context.BasketProducts.FindAsync(BasketProductsId);

            if (item != null)
            {
                _context.BasketProducts.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Baskets");
        }

        // POST: /basket/update — updates the quantity of a basket item
        // Removes the item entirely if quantity is set to 0 or below
        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> Update(int BasketProductsId, int Quantity)
        {
            var item = await _context.BasketProducts.FindAsync(BasketProductsId);

            if (item != null)
            {
                if (Quantity <= 0)
                {
                    // Remove item if quantity drops to zero
                    _context.BasketProducts.Remove(item);
                }
                else
                {
                    item.Quantity = Quantity;
                }

                await _context.SaveChangesAsync();
            }

            // Return Ok so the AJAX auto-update on the basket page doesn't redirect
            return Ok();
        }

        // POST: /basket/clear — removes all items from the user's active basket
        [HttpPost]
        [Route("clear")]
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Load basket with all its products
            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Status == true);

            if (basket != null)
            {
                _context.BasketProducts.RemoveRange(basket.BasketProducts);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Baskets");
        }
    }
}