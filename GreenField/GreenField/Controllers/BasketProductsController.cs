using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;

namespace GreenField.Controllers
{
    // raw basket product management — admin only, mainly for debugging
    [Authorize(Roles = "Admin")]
    public class BasketProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // inject db context
        public BasketProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET — lists every basket product across all users with basket and product details
        public async Task<IActionResult> Index()
        {
            var basketProducts = await _context.BasketProducts
                .Include(b => b.Basket)
                .Include(b => b.Products)
                    .ThenInclude(p => p.Producers)
                .ToListAsync();

            return View(basketProducts);
        }

        // GET — details for a single basket product record
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var basketProduct = await _context.BasketProducts
                .Include(b => b.Basket)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.BasketProductsId == id);

            if (basketProduct == null) return NotFound();
            return View(basketProduct);
        }

        // POST — removes a basket product record, used by admin to clean up if needed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var basketProduct = await _context.BasketProducts.FindAsync(id);
            if (basketProduct != null)
                _context.BasketProducts.Remove(basketProduct);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Basket product removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
