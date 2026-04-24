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

        // inject db and user manager
        public ProducersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET — public producers listing with their products included
        public async Task<IActionResult> Index()
        {
            var producers = await _context.Producers
                .Include(p => p.Products)
                .ToListAsync();

            return View(producers);
        }

        // GET — public producer detail page, only shows available products
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producer = await _context.Producers
                .Include(p => p.Products.Where(pr => pr.IsAvailable))
                .FirstOrDefaultAsync(m => m.ProducersId == id);

            if (producer == null) return NotFound();
            return View(producer);
        }

        // GET — create producer form, admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // load all users in the Producer role so admin can assign one from a dropdown
            var producerUsers = await _userManager.GetUsersInRoleAsync("Producer");
            ViewBag.UserList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                producerUsers.Select(u => new { u.Id, Display = u.Email ?? u.UserName }),
                "Id", "Display");
            return View();
        }

        // POST — saves the new producer, admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("UserId,BusinessName,BusinessDescription,BusinessBasedIn,Logo")] Producers producers)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producers);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producer created.";
                return RedirectToAction(nameof(Index));
            }

            // re-populate the user dropdown if validation failed
            var producerUsers = await _userManager.GetUsersInRoleAsync("Producer");
            ViewBag.UserList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                producerUsers.Select(u => new { u.Id, Display = u.Email ?? u.UserName }),
                "Id", "Display", producers.UserId);
            return View(producers);
        }

        // GET — edit producer form, admin or the producer themselves
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producer = await _context.Producers.FindAsync(id);
            if (producer == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // producers can only edit their own profile
            if (User.IsInRole("Producer") && producer.UserId != userId)
                return Forbid();

            // admins get a user reassignment dropdown, producers don't see it
            if (User.IsInRole("Admin"))
            {
                var producerUsers = await _userManager.GetUsersInRoleAsync("Producer");
                ViewBag.UserList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    producerUsers.Select(u => new { u.Id, Display = u.Email ?? u.UserName }),
                    "Id", "Display", producer.UserId);
            }

            return View(producer);
        }

        // POST — saves edits to a producer, admin or own producer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> Edit(int id, [Bind("ProducersId,UserId,BusinessName,BusinessDescription,BusinessBasedIn,Logo")] Producers producers)
        {
            if (id != producers.ProducersId) return NotFound();

            var userId = _userManager.GetUserId(User);

            // producers can't reassign their profile to a different user
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

        // GET — delete confirmation page, admin only
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

        // POST — actually deletes the producer after confirmation, admin only
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
