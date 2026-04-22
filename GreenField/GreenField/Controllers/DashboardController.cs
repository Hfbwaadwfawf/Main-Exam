using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using GreenField.Models.ViewModels;

namespace GreenField.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? filterProducerId)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (User.IsInRole("Standard"))
                return await StandardDashboard(userId, user);

            if (User.IsInRole("Producer"))
                return await ProducerDashboard(userId, user);

            if (User.IsInRole("Admin"))
                return await AdminDashboard(userId, user, filterProducerId);

            return Forbid();
        }

        // ───────────────── Standard ─────────────────

        private async Task<IActionResult> StandardDashboard(string userId, IdentityUser user)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var loyalty = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            int points = loyalty?.Points ?? 0;
            int pointsToNextReward = 300 - (points % 300);

            var vm = new StandardDashboardViewModel
            {
                UserName = user.UserName ?? user.Email ?? "User",
                Email = user.Email ?? "",
                Points = points,
                PointsToNextReward = pointsToNextReward,
                Orders = orders
            };

            return View("StandardDashboard", vm);
        }

        // ───────────────── Producer ─────────────────

        private async Task<IActionResult> ProducerDashboard(string userId, IdentityUser user)
        {
            var producer = await _context.Producers
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null)
            {
                TempData["Error"] = "No producer profile found.";
                return RedirectToAction("Index", "Home");
            }

            var producerOrders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == producer.ProducersId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var vm = new ProducerDashboardViewModel
            {
                UserName = user.UserName ?? user.Email ?? "Producer",
                Producer = producer,
                Products = producer.Products?.ToList() ?? new List<Products>(),
                Orders = producerOrders
            };

            return View("ProducerDashboard", vm);
        }

        // ───────────────── Admin ─────────────────

        private async Task<IActionResult> AdminDashboard(string userId, IdentityUser user, int? filterProducerId)
        {
            var allProducers = await _context.Producers
                .Include(p => p.Products)
                .ToListAsync();

            Producers? selectedProducer = null;
            List<Products> filteredProducts = new();
            List<Orders> filteredOrders = new();

            if (filterProducerId.HasValue)
            {
                selectedProducer = allProducers.FirstOrDefault(p => p.ProducersId == filterProducerId.Value);

                if (selectedProducer != null)
                {
                    filteredProducts = selectedProducer.Products?.ToList() ?? new();

                    filteredOrders = await _context.Orders
                        .Include(o => o.OrderProducts)
                            .ThenInclude(op => op.Products)
                        .Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == filterProducerId.Value))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();
                }
            }
            else
            {
                // No filter — show all products and orders
                filteredProducts = allProducers.SelectMany(p => p.Products ?? new List<Products>()).ToList();

                filteredOrders = await _context.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Products)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            var vm = new AdminDashboardViewModel
            {
                UserName = user.UserName ?? user.Email ?? "Admin",
                AllProducers = allProducers,
                SelectedProducer = selectedProducer,
                FilterProducerId = filterProducerId,
                Products = filteredProducts,
                Orders = filteredOrders
            };

            return View("AdminDashboard", vm);
        }

        // ───────────────── Profile Update (Standard) ─────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Standard")]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, string defaultDeliveryAddress)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            // Store extra profile fields — extend IdentityUser or use a UserProfile table as needed
            // For now, update PhoneNumber via UserManager
            user.PhoneNumber = phoneNumber;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
