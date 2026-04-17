using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    public class ProductStampsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductStampsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProductStamps
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ProductStamps.Include(p => p.Products).Include(p => p.Stamps);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ProductStamps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productStamps = await _context.ProductStamps
                .Include(p => p.Products)
                .Include(p => p.Stamps)
                .FirstOrDefaultAsync(m => m.ProductStampsId == id);
            if (productStamps == null)
            {
                return NotFound();
            }

            return View(productStamps);
        }

        // GET: ProductStamps/Create
        public IActionResult Create()
        {
            ViewData["ProductsId"] = new SelectList(_context.Products, "ProductsId", "ProductsId");
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId");
            return View();
        }

        // POST: ProductStamps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductStampsId,ProductsId,StampsId")] ProductStamps productStamps)
        {
            if (ModelState.IsValid)
            {
                _context.Add(productStamps);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductsId"] = new SelectList(_context.Products, "ProductsId", "ProductsId", productStamps.ProductsId);
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId", productStamps.StampsId);
            return View(productStamps);
        }

        // GET: ProductStamps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productStamps = await _context.ProductStamps.FindAsync(id);
            if (productStamps == null)
            {
                return NotFound();
            }
            ViewData["ProductsId"] = new SelectList(_context.Products, "ProductsId", "ProductsId", productStamps.ProductsId);
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId", productStamps.StampsId);
            return View(productStamps);
        }

        // POST: ProductStamps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductStampsId,ProductsId,StampsId")] ProductStamps productStamps)
        {
            if (id != productStamps.ProductStampsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productStamps);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductStampsExists(productStamps.ProductStampsId))
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
            ViewData["ProductsId"] = new SelectList(_context.Products, "ProductsId", "ProductsId", productStamps.ProductsId);
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId", productStamps.StampsId);
            return View(productStamps);
        }

        // GET: ProductStamps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productStamps = await _context.ProductStamps
                .Include(p => p.Products)
                .Include(p => p.Stamps)
                .FirstOrDefaultAsync(m => m.ProductStampsId == id);
            if (productStamps == null)
            {
                return NotFound();
            }

            return View(productStamps);
        }

        // POST: ProductStamps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productStamps = await _context.ProductStamps.FindAsync(id);
            if (productStamps != null)
            {
                _context.ProductStamps.Remove(productStamps);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductStampsExists(int id)
        {
            return _context.ProductStamps.Any(e => e.ProductStampsId == id);
        }
    }
}
