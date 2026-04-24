using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    // whole controller is admin only
    [Authorize(Roles = "Admin")]
    public class DiscountCodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        // inject db context
        public DiscountCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET — list all discount codes sorted alphabetically
        public async Task<IActionResult> Index()
        {
            return View(await _context.DiscountCodes.OrderBy(d => d.Code).ToListAsync());
        }

        // GET — details for a single code
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var code = await _context.DiscountCodes
                .FirstOrDefaultAsync(m => m.DiscountCodesId == id);

            if (code == null) return NotFound();
            return View(code);
        }

        // GET — create new discount code form
        public IActionResult Create()
        {
            return View();
        }

        // POST — saves a new discount code with validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Percentage,IsActive")] DiscountCodes discountCodes)
        {
            // make sure the code field isn't blank
            if (string.IsNullOrWhiteSpace(discountCodes.Code))
                ModelState.AddModelError("Code", "Code cannot be empty.");

            // percentage must be a sensible value
            if (discountCodes.Percentage <= 0 || discountCodes.Percentage > 100)
                ModelState.AddModelError("Percentage", "Percentage must be between 1 and 100.");

            // don't allow duplicate codes
            if (await _context.DiscountCodes.AnyAsync(d => d.Code == discountCodes.Code))
                ModelState.AddModelError("Code", "A code with this name already exists.");

            if (ModelState.IsValid)
            {
                _context.Add(discountCodes);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Discount code created.";
                return RedirectToAction(nameof(Index));
            }
            return View(discountCodes);
        }

        // GET — edit discount code form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var code = await _context.DiscountCodes.FindAsync(id);
            if (code == null) return NotFound();
            return View(code);
        }

        // POST — saves changes to an existing discount code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DiscountCodesId,Code,Percentage,IsActive")] DiscountCodes discountCodes)
        {
            if (id != discountCodes.DiscountCodesId) return NotFound();

            // validate percentage range
            if (discountCodes.Percentage <= 0 || discountCodes.Percentage > 100)
                ModelState.AddModelError("Percentage", "Percentage must be between 1 and 100.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discountCodes);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Discount code updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    // handle the case where the code was deleted by someone else between GET and POST
                    if (!_context.DiscountCodes.Any(d => d.DiscountCodesId == discountCodes.DiscountCodesId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(discountCodes);
        }

        // GET — delete confirmation page
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var code = await _context.DiscountCodes.FirstOrDefaultAsync(m => m.DiscountCodesId == id);
            if (code == null) return NotFound();
            return View(code);
        }

        // POST — deletes the discount code after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var code = await _context.DiscountCodes.FindAsync(id);
            if (code != null)
                _context.DiscountCodes.Remove(code);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Discount code deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST — quickly toggles a code between active and inactive without going to the edit page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var code = await _context.DiscountCodes.FindAsync(id);
            if (code == null) return NotFound();

            code.IsActive = !code.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Code {code.Code} is now {(code.IsActive ? "active" : "inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
