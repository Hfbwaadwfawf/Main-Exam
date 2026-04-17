using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using System.Security.Claims;

namespace GreenField.Controllers
{
    [Route("basket")]
    public class BasketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BasketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /basket
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

            var basketProducts = await _context.BasketProducts
                .Where(x => x.BasketId == basket.BasketId)
                .Include(x => x.Basket)
                .Include(x => x.Products)
                .ToListAsync();

            decimal subtotal = 0m;

            foreach (var basketProduct in basketProducts)
            {
                var productTotal = basketProduct.Products.Price * basketProduct.Quantity;
                subtotal += productTotal;
            }

            var orderCount = await _context.Orders.CountAsync(x => x.UserId == userId);

            decimal discount = 0m;

            if (orderCount >= 5)
            {
                discount = subtotal * 0.10m;
            }

            decimal total = subtotal - discount;

            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.Total = total;
            ViewBag.OrderCount = orderCount;

            return View(basketProducts);
        }

        // POST: /basket/create
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(int ProductsId)
        {
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

            var basketProduct = await _context.BasketProducts
                .FirstOrDefaultAsync(bp => bp.BasketId == basket.BasketId && bp.ProductsId == ProductsId);

            if (basketProduct != null)
            {
                basketProduct.Quantity++;
            }
            else
            {
                basketProduct = new BasketProducts
                {
                    BasketId = basket.BasketId,
                    ProductsId = ProductsId,
                    Quantity = 1
                };

                _context.BasketProducts.Add(basketProduct);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: /basket/remove
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

        // POST: /basket/update
        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> Update(int BasketProductsId, int Quantity)
        {
            var item = await _context.BasketProducts.FindAsync(BasketProductsId);

            if (item != null)
            {
                if (Quantity <= 0)
                {
                    _context.BasketProducts.Remove(item);
                }
                else
                {
                    item.Quantity = Quantity;
                }

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        // POST: /basket/clear
        [HttpPost]
        [Route("clear")]
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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