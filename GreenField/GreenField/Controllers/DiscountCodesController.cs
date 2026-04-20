using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DiscountCodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DiscountCodes
        public async Task<IActionResult> Index()
        {
            return View(await _context.DiscountCodes.OrderBy(d => d.Code).ToListAsync());
        }

        // GET: DiscountCodes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var code = await _context.DiscountCodes
                .FirstOrDefaultAsync(m => m.DiscountCodesId == id);

            if (code == null) return NotFound();
            return View(code);
        }

        // GET: DiscountCodes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DiscountCodes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Percentage,IsActive")] DiscountCodes discountCodes)
        {
            if (string.IsNullOrWhiteSpace(discountCodes.Code))
                ModelState.AddModelError("Code", "Code cannot be empty.");

            if (discountCodes.Percentage <= 0 || discountCodes.Percentage > 100)
                ModelState.AddModelError("Percentage", "Percentage must be between 1 and 100.");

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

        // GET: DiscountCodes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var code = await _context.DiscountCodes.FindAsync(id);
            if (code == null) return NotFound();
            return View(code);
        }

        // POST: DiscountCodes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DiscountCodesId,Code,Percentage,IsActive")] DiscountCodes discountCodes)
        {
            if (id != discountCodes.DiscountCodesId) return NotFound();

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
                    if (!_context.DiscountCodes.Any(d => d.DiscountCodesId == discountCodes.DiscountCodesId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(discountCodes);
        }

        // GET: DiscountCodes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var code = await _context.DiscountCodes.FirstOrDefaultAsync(m => m.DiscountCodesId == id);
            if (code == null) return NotFound();
            return View(code);
        }

        // POST: DiscountCodes/Delete/5
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

        // POST: DiscountCodes/Toggle/5
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