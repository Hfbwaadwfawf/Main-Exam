using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")] // Discount code management is admin only
    public class DiscountCodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DiscountCodes — list all discount codes
        public async Task<IActionResult> Index()
        {
            return View(await _context.DiscountCodes.ToListAsync());
        }

        // GET: DiscountCodes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCodes = await _context.DiscountCodes
                .FirstOrDefaultAsync(m => m.DiscountCodesId == id);
            if (discountCodes == null)
            {
                return NotFound();
            }

            return View(discountCodes);
        }

        // GET: DiscountCodes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DiscountCodes/Create — saves a new discount code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DiscountCodesId,Code,Percentage,IsActive")] DiscountCodes discountCodes)
        {
            if (ModelState.IsValid)
            {
                _context.Add(discountCodes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(discountCodes);
        }

        // GET: DiscountCodes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCodes = await _context.DiscountCodes.FindAsync(id);
            if (discountCodes == null)
            {
                return NotFound();
            }
            return View(discountCodes);
        }

        // POST: DiscountCodes/Edit/5 — saves changes to an existing discount code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DiscountCodesId,Code,Percentage,IsActive")] DiscountCodes discountCodes)
        {
            if (id != discountCodes.DiscountCodesId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discountCodes);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountCodesExists(discountCodes.DiscountCodesId))
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
            return View(discountCodes);
        }

        // GET: DiscountCodes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCodes = await _context.DiscountCodes
                .FirstOrDefaultAsync(m => m.DiscountCodesId == id);
            if (discountCodes == null)
            {
                return NotFound();
            }

            return View(discountCodes);
        }

        // POST: DiscountCodes/Delete/5 — confirms and deletes a discount code
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var discountCodes = await _context.DiscountCodes.FindAsync(id);
            if (discountCodes != null)
            {
                _context.DiscountCodes.Remove(discountCodes);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper — checks if a discount code exists by ID
        private bool DiscountCodesExists(int id)
        {
            return _context.DiscountCodes.Any(e => e.DiscountCodesId == id);
        }
    }
}