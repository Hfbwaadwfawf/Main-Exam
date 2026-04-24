using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // inject db and user manager
        public ProductsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET — store page, supports filtering by search, producer, category and price range
        public async Task<IActionResult> Index(string? search, int? producerId, string? category, decimal? minPrice, decimal? maxPrice)
        {
            // start with all available products including their producer
            var query = _context.Products
                .Include(p => p.Producers)
                .Where(p => p.IsAvailable)
                .AsQueryable();

            // apply each filter if provided
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search));

            if (producerId.HasValue)
                query = query.Where(p => p.ProducersId == producerId);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.category == category);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice);

            // pass filter values to the view so the sidebar stays populated
            ViewData["Producers"] = new SelectList(await _context.Producers.ToListAsync(), "ProducersId", "BusinessName");
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentProducer"] = producerId?.ToString();
            ViewData["CurrentCategory"] = category;
            ViewData["CurrentMinPrice"] = minPrice?.ToString();
            ViewData["CurrentMaxPrice"] = maxPrice?.ToString();

            return View(await query.ToListAsync());
        }

        // GET — product detail page, public
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Producers)
                .FirstOrDefaultAsync(m => m.ProductsId == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // GET — create product form, admin and producer only
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                // producers can only create products for their own business
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null)
                {
                    TempData["Error"] = "No producer profile found for your account.";
                    return RedirectToAction(nameof(Index));
                }

                // pass the producer id so the hidden field in the view has the right value
                ViewBag.ProducerOwnId = producer.ProducersId;
                ViewData["ProducersId"] = new SelectList(
                    new List<Producers> { producer }, "ProducersId", "BusinessName", producer.ProducersId);
            }
            else
            {
                // admin sees all producers in the dropdown
                ViewData["ProducersId"] = new SelectList(
                    await _context.Producers.ToListAsync(), "ProducersId", "BusinessName");
            }

            return View();
        }

        // POST — saves a new product, admin and producer only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Create([Bind("ProducersId,ProductName,Description,category,Price,Stock,IsAvailable,Image")] Products products)
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                // make sure the producer isn't trying to create a product under a different producer
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || products.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(products);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product created.";
                return RedirectToAction(nameof(Index));
            }

            // re-populate the dropdown if validation failed and we return to the form
            ViewData["ProducersId"] = new SelectList(
                await _context.Producers.ToListAsync(), "ProducersId", "BusinessName", products.ProducersId);
            return View(products);
        }

        // GET — edit product form, admin and producer only
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Producers)
                .FirstOrDefaultAsync(p => p.ProductsId == id);

            if (product == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                // producers can only edit their own products
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || product.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            ViewData["ProducersId"] = new SelectList(
                await _context.Producers.ToListAsync(), "ProducersId", "BusinessName", product.ProducersId);
            return View(product);
        }

        // POST — saves edits to a product, admin and producer only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int id, [Bind("ProductsId,ProducersId,ProductName,Description,category,Price,Stock,IsAvailable,Image")] Products products)
        {
            if (id != products.ProductsId) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                // double check producer can't reassign the product to someone else
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || products.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // auto mark as unavailable if stock runs out
                    if (products.Stock <= 0)
                        products.IsAvailable = false;

                    _context.Update(products);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Product updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    // if the product no longer exists, return 404
                    if (!_context.Products.Any(p => p.ProductsId == products.ProductsId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProducersId"] = new SelectList(
                await _context.Producers.ToListAsync(), "ProducersId", "BusinessName", products.ProducersId);
            return View(products);
        }

        // GET — delete confirmation page, admin and producer only
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Producers)
                .FirstOrDefaultAsync(m => m.ProductsId == id);

            if (product == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                // producers can only delete their own products
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || product.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            return View(product);
        }

        // POST — actually deletes the product after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer"))
            {
                // one last ownership check before deleting
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || product.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Product deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
