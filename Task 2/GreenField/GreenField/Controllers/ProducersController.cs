using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using System.Security.Claims;

namespace GreenField.Controllers
{
    public class ProducersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProducersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Producers.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producers = await _context.Producers
                .FirstOrDefaultAsync(m => m.ProducersId == id);

            if (producers == null) return NotFound();

            return View(producers);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BusinessName,BusinessDescription,BusinessBasedIn,Logo")] Producers producers)
        {
            if (ModelState.IsValid)
            {
                producers.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                _context.Add(producers);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(producers);
        }

        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producers = await _context.Producers.FindAsync(id);
            if (producers == null) return NotFound();

            return View(producers);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Producer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProducersId,BusinessName,BusinessDescription,BusinessBasedIn,Logo")] Producers producers)
        {
            if (id != producers.ProducersId) return NotFound();

            var existing = await _context.Producers.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProducersId == id);

            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    producers.UserId = existing.UserId;

                    _context.Update(producers);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProducersExists(producers.ProducersId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producers);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producers = await _context.Producers
                .FirstOrDefaultAsync(m => m.ProducersId == id);

            if (producers == null) return NotFound();

            return View(producers);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producers = await _context.Producers.FindAsync(id);
            if (producers != null)
            {
                _context.Producers.Remove(producers);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProducersExists(int id)
        {
            return _context.Producers.Any(e => e.ProducersId == id);
        }
    }
}