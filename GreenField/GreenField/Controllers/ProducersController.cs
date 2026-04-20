using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    public class ProducersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProducersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Producers — public
        public async Task<IActionResult> Index()
        {
            var producers = await _context.Producers
                .Include(p => p.Products)
                .ToListAsync();

            return View(producers);
        }

        // GET: Producers/Details/5 — public
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producer = await _context.Producers
                .Include(p => p.Products.Where(pr => pr.IsAvailable))
                .FirstOrDefaultAsync(m => m.ProducersId == id);

            if (producer == null) return NotFound();
            return View(producer);
        }

        // GET: Producers/Create — Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Producers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("UserId,BusinessName,BusinessDescription,BusinessBasedIn,LogoPath")] Producers producers)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producers);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producer created.";
                return RedirectToAction(nameof(Index));
            }
            return View(producers);
        }

        // GET: Producers/Edit/5 — Admin or own Producer
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producer = await _context.Producers.FindAsync(id);
            if (producer == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer") && producer.UserId != userId)
                return Forbid();

            return View(producer);
        }

        // POST: Producers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int id, [Bind("ProducersId,UserId,BusinessName,BusinessDescription,BusinessBasedIn,LogoPath")] Producers producers)
        {
            if (id != producers.ProducersId) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Producer") && producers.UserId != userId)
                return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producers);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Producer profile updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Producers.Any(p => p.ProducersId == producers.ProducersId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producers);
        }

        // GET: Producers/Delete/5 — Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producer = await _context.Producers
                .Include(p => p.Products)
                .FirstOrDefaultAsync(m => m.ProducersId == id);

            if (producer == null) return NotFound();
            return View(producer);
        }

        // POST: Producers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producer = await _context.Producers.FindAsync(id);
            if (producer != null)
                _context.Producers.Remove(producer);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Producer removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}