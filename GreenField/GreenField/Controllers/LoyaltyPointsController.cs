using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    // whole controller requires login
    [Authorize]
    public class LoyaltyPointsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // inject db and user manager
        public LoyaltyPointsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET — shows the user their points balance and what rewards they can unlock
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // admins see a list of all users' points records
            if (User.IsInRole("Admin"))
                return View("AdminIndex", await _context.LoyaltyPoints.ToListAsync());

            // find the user's points record
            var myPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            // create a record with 0 points if they don't have one yet
            if (myPoints == null)
            {
                myPoints = new LoyaltyPoints { UserId = userId, Points = 0 };
                _context.LoyaltyPoints.Add(myPoints);
                await _context.SaveChangesAsync();
            }

            // grab all the loyalty reward codes (codes with a points requirement) so the view can show progress
            var rewardCodes = await _context.DiscountCodes
                .Where(d => d.PointsRequired > 0 && d.IsActive)
                .OrderBy(d => d.PointsRequired)
                .ToListAsync();

            ViewBag.RewardCodes = rewardCodes;
            ViewBag.UserPoints = myPoints.Points;

            return View(myPoints);
        }

        // GET — details for a single user's points record, admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loyaltyPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(m => m.LoyaltyPointsId == id);

            if (loyaltyPoints == null) return NotFound();
            return View(loyaltyPoints);
        }

        // POST — admin can manually add or subtract points from a user's balance
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Adjust(int loyaltyPointsId, int adjustment)
        {
            var record = await _context.LoyaltyPoints.FindAsync(loyaltyPointsId);
            if (record == null) return NotFound();

            // make sure points can't go below 0
            record.Points = Math.Max(0, record.Points + adjustment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Points adjusted.";
            return RedirectToAction(nameof(Index));
        }

        // POST — admin can delete a user's points record entirely
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
