using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")] // Raw order product management is admin only
    public class OrderProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OrderProducts — list all order product line items across all orders
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.OrderProducts.Include(o => o.Orders).Include(o => o.Products);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: OrderProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProducts = await _context.OrderProducts
                .Include(o => o.Orders)
                .Include(o => o.Products)
                .FirstOrDefaultAsync(m => m.OrderProductsId == id);
            if (orderProducts == null)
            {
                return NotFound();
            }

            return View(orderProducts);
        }

        // GET: OrderProducts/Create — manually add a product line to an order
        public IActionResult Create()
        {
            ViewData["OrdersId"] = new SelectList(_context.Set<Orders>(), "OrdersId", "OrdersId");
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId");
            return View();
        }

        // POST: OrderProducts/Create — saves a manually created order product entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderProductsId,ProductsId,OrdersId,Quantity")] OrderProducts orderProducts)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orderProducts);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrdersId"] = new SelectList(_context.Set<Orders>(), "OrdersId", "OrdersId", orderProducts.OrdersId);
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId", orderProducts.ProductsId);
            return View(orderProducts);
        }

        // GET: OrderProducts/Edit/5 — load an order product line for editing
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProducts = await _context.OrderProducts.FindAsync(id);
            if (orderProducts == null)
            {
                return NotFound();
            }
            ViewData["OrdersId"] = new SelectList(_context.Set<Orders>(), "OrdersId", "OrdersId", orderProducts.OrdersId);
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId", orderProducts.ProductsId);
            return View(orderProducts);
        }

        // POST: OrderProducts/Edit/5 — saves changes to an order product line
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderProductsId,ProductsId,OrdersId,Quantity")] OrderProducts orderProducts)
        {
            if (id != orderProducts.OrderProductsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderProducts);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderProductsExists(orderProducts.OrderProductsId))
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
            ViewData["OrdersId"] = new SelectList(_context.Set<Orders>(), "OrdersId", "OrdersId", orderProducts.OrdersId);
            ViewData["ProductsId"] = new SelectList(_context.Set<Products>(), "ProductsId", "ProductsId", orderProducts.ProductsId);
            return View(orderProducts);
        }

        // GET: OrderProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProducts = await _context.OrderProducts
                .Include(o => o.Orders)
                .Include(o => o.Products)
                .FirstOrDefaultAsync(m => m.OrderProductsId == id);
            if (orderProducts == null)
            {
                return NotFound();
            }

            return View(orderProducts);
        }

        // POST: OrderProducts/Delete/5 — confirms and removes an order product line
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderProducts = await _context.OrderProducts.FindAsync(id);
            if (orderProducts != null)
            {
                _context.OrderProducts.Remove(orderProducts);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper — checks if an order product record exists by ID
        private bool OrderProductsExists(int id)
        {
            return _context.OrderProducts.Any(e => e.OrderProductsId == id);
        }
    }
}