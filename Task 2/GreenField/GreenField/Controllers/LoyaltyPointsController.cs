using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")] // Loyalty points management is admin only
    public class LoyaltyPointsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoyaltyPointsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LoyaltyPoints — list all user loyalty point records
        public async Task<IActionResult> Index()
        {
            return View(await _context.LoyaltyPoints.ToListAsync());
        }

        // GET: LoyaltyPoints/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loyaltyPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(m => m.LoyaltyPointsId == id);
            if (loyaltyPoints == null)
            {
                return NotFound();
            }

            return View(loyaltyPoints);
        }

        // GET: LoyaltyPoints/Create — manually create a points record
        public IActionResult Create()
        {
            return View();
        }

        // POST: LoyaltyPoints/Create — saves a manually created loyalty points record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LoyaltyPointsId,UserId,Points")] LoyaltyPoints loyaltyPoints)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loyaltyPoints);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loyaltyPoints);
        }

        // GET: LoyaltyPoints/Edit/5 — load a points record for editing
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loyaltyPoints = await _context.LoyaltyPoints.FindAsync(id);
            if (loyaltyPoints == null)
            {
                return NotFound();
            }
            return View(loyaltyPoints);
        }

        // POST: LoyaltyPoints/Edit/5 — saves changes to a loyalty points record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LoyaltyPointsId,UserId,Points")] LoyaltyPoints loyaltyPoints)
        {
            if (id != loyaltyPoints.LoyaltyPointsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loyaltyPoints);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoyaltyPointsExists(loyaltyPoints.LoyaltyPointsId))
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
            return View(loyaltyPoints);
        }

        // GET: LoyaltyPoints/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loyaltyPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(m => m.LoyaltyPointsId == id);
            if (loyaltyPoints == null)
            {
                return NotFound();
            }

            return View(loyaltyPoints);
        }

        // POST: LoyaltyPoints/Delete/5 — confirms and deletes a loyalty points record
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loyaltyPoints = await _context.LoyaltyPoints.FindAsync(id);
            if (loyaltyPoints != null)
            {
                _context.LoyaltyPoints.Remove(loyaltyPoints);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper — checks if a loyalty points record exists by ID
        private bool LoyaltyPointsExists(int id)
        {
            return _context.LoyaltyPoints.Any(e => e.LoyaltyPointsId == id);
        }
    }
}