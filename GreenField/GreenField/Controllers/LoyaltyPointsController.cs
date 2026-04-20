using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize]
    public class LoyaltyPointsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LoyaltyPointsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: LoyaltyPoints
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin"))
            {
                return View(await _context.LoyaltyPoints.ToListAsync());
            }

            var myPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            if (myPoints == null)
            {
                myPoints = new LoyaltyPoints { UserId = userId, Points = 0 };
                _context.LoyaltyPoints.Add(myPoints);
                await _context.SaveChangesAsync();
            }

            return View(new List<LoyaltyPoints> { myPoints });
        }

        // GET: LoyaltyPoints/Details/5 — Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loyaltyPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(m => m.LoyaltyPointsId == id);

            if (loyaltyPoints == null) return NotFound();
            return View(loyaltyPoints);
        }

        // POST: LoyaltyPoints/Adjust — Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adjust(int loyaltyPointsId, int adjustment)
        {
            var record = await _context.LoyaltyPoints.FindAsync(loyaltyPointsId);
            if (record == null) return NotFound();

            record.Points = Math.Max(0, record.Points + adjustment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Points adjusted.";
            return RedirectToAction(nameof(Index));
        }

        // POST: LoyaltyPoints/Delete/5 — Admin only
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.LoyaltyPoints.FindAsync(id);
            if (record != null)
                _context.LoyaltyPoints.Remove(record);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Points record deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}