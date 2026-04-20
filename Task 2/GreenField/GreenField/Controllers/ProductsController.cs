using Microsoft.AspNetCore.Authorization;
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

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products — public, anyone can browse the shop
        public async Task<IActionResult> Index(string search, decimal? minPrice, decimal? maxPrice, int? producer, string sort)
        {
            var query = _context.Products
                .Include(p => p.Producers)
                .Include(p => p.ProductStamps)
                    .ThenInclude(ps => ps.Stamps)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search) || (p.Description != null && p.Description.Contains(search)));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (producer.HasValue)
                query = query.Where(p => p.ProducersId == producer.Value);

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.ProductName),
                "stock" => query.OrderByDescending(p => p.Stock),
                _ => query.OrderBy(p => p.ProductName)
            };

            ViewBag.Producers = await _context.Producers.ToListAsync();

            return View(await query.ToListAsync());
        }

        // GET: Products/Details/5 — public
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var products = await _context.Products
                .Include(p => p.Producers)
                .FirstOrDefaultAsync(m => m.ProductsId == id);

            if (products == null)
                return NotFound();

            return View(products);
        }

        // GET: Products/Create — admin and producer only
        [Authorize(Roles = "Admin,Producer")]
        public IActionResult Create()
        {
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "BusinessName");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Producer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductsId,ProducersId,ProductName,Description,Price,Stock,IsAvailable,Image")] Products products)
        {
            if (ModelState.IsValid)
            {
                _context.Add(products);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "BusinessName", products.ProducersId);
            return View(products);
        }

        // GET: Products/Edit/5 — admin and producer only
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var products = await _context.Products.FindAsync(id);
            if (products == null)
                return NotFound();

            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "BusinessName", products.ProducersId);
            return View(products);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Producer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductsId,ProducersId,ProductName,Description,Price,Stock,IsAvailable,Image")] Products products)
        {
            if (id != products.ProductsId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(products);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductsExists(products.ProductsId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "BusinessName", products.ProducersId);
            return View(products);
        }

        // GET: Products/Delete/5 — admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var products = await _context.Products
                .Include(p => p.Producers)
                .FirstOrDefaultAsync(m => m.ProductsId == id);

            if (products == null)
                return NotFound();

            return View(products);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var products = await _context.Products.FindAsync(id);
            if (products != null)
                _context.Products.Remove(products);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductsExists(int id)
        {
            return _context.Products.Any(e => e.ProductsId == id);
        }
    }
}