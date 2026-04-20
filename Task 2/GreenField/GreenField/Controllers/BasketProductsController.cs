using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")] // Raw basket product management is admin only
    public class BasketProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BasketProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BasketProducts — list all basket product records across all users
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BasketProducts.Include(b => b.Basket).Include(b => b.Products);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: BasketProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var basketProducts = await _context.BasketProducts
                .Include(b => b.Basket)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.BasketProductsId == id);
            if (basketProducts == null)
            {
                return NotFound();
            }

            return View(basketProducts);
        }

        // GET: BasketProducts/Create — manually add a product to a basket
        public IActionResult Create()
        {
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId");
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId");
            return View();
        }

        // POST: BasketProducts/Create — saves a manually created basket product entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BasketProductsId,BasketId,ProductsId,Quantity")] BasketProducts basketProducts)
        {
            if (ModelState.IsValid)
            {
                _context.Add(basketProducts);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProducts.BasketId);
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId", basketProducts.ProductsId);
            return View(basketProducts);
        }

        // GET: BasketProducts/Edit/5 — load a basket product for editing
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var basketProducts = await _context.BasketProducts.FindAsync(id);
            if (basketProducts == null)
            {
                return NotFound();
            }
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProducts.BasketId);
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId", basketProducts.ProductsId);
            return View(basketProducts);
        }

        // POST: BasketProducts/Edit/5 — saves changes to a basket product entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BasketProductsId,BasketId,ProductsId,Quantity")] BasketProducts basketProducts)
        {
            if (id != basketProducts.BasketProductsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(basketProducts);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BasketProductsExists(basketProducts.BasketProductsId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BasketId"] = new SelectList(_context.Basket, "BasketId", "BasketId", basketProducts.BasketId);
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId", basketProducts.ProductsId);
            return View(basketProducts);
        }

        // GET: BasketProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var basketProducts = await _context.BasketProducts
                .Include(b => b.Basket)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.BasketProductsId == id);
            if (basketProducts == null)
            {
                return NotFound();
            }

            return View(basketProducts);
        }

        // POST: BasketProducts/Delete/5 — confirms and removes a basket product entry
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var basketProducts = await _context.BasketProducts.FindAsync(id);
            if (basketProducts != null)
            {
                _context.BasketProducts.Remove(basketProducts);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper — checks if a basket product record exists by ID
        private bool BasketProductsExists(int id)
        {
            return _context.BasketProducts.Any(e => e.BasketProductsId == id);
        }
    }
}