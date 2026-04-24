using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;
using GreenField.Models.ViewModels;

namespace GreenField.Controllers
{
    // whole controller requires login
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // inject db and user manager
        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // entry point — figures out which dashboard to show based on the user's role
        public async Task<IActionResult> Index(int? filterProducerId)
        {
            var userId = _userManager.GetUserId(User);

            // if somehow userId is null, just send them home
            if (userId == null) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Index", "Home");

            if (User.IsInRole("Standard"))
                return await StandardDashboard(userId, user);

            if (User.IsInRole("Producer"))
                return await ProducerDashboard(userId, user);

            if (User.IsInRole("Admin"))
                return await AdminDashboard(userId, user, filterProducerId);

            // user is logged in but has no role yet — send home with a message
            TempData["Error"] = "Your account doesn't have a role assigned yet. Please contact support.";
            return RedirectToAction("Index", "Home");
        }

        // builds the view model for a standard user and returns the standard dashboard view
        private async Task<IActionResult> StandardDashboard(string userId, IdentityUser user)
        {
            // load all orders for this user with their products
            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // get their loyalty points record if it exists
            var loyalty = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            int points = loyalty?.Points ?? 0;
            // how many points until the next 300-point reward tier
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

        // builds the view model for a producer and returns the producer dashboard view
        private async Task<IActionResult> ProducerDashboard(string userId, IdentityUser user)
        {
            // find the producer profile linked to this user
            var producer = await _context.Producers
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // if they don't have a producer profile yet, send them home
            if (producer == null)
            {
                TempData["Error"] = "No producer profile found.";
                return RedirectToAction("Index", "Home");
            }

            // load all orders that contain at least one of this producer's products
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

        // builds the view model for an admin — supports filtering by producer
        private async Task<IActionResult> AdminDashboard(string userId, IdentityUser user, int? filterProducerId)
        {
            // load all producers with their products
            var allProducers = await _context.Producers
                .Include(p => p.Products)
                .ToListAsync();

            Producers? selectedProducer = null;
            List<Products> filteredProducts = new();
            List<Orders> filteredOrders = new();

            if (filterProducerId.HasValue)
            {
                // a specific producer filter is applied
                selectedProducer = allProducers.FirstOrDefault(p => p.ProducersId == filterProducerId.Value);

                if (selectedProducer != null)
                {
                    // only show that producer's products
                    filteredProducts = selectedProducer.Products?.ToList() ?? new();

                    // only show orders that contain at least one product from this producer
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
                // no filter — show everything
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

        // POST — standard user can update their phone number from the profile tab
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Standard")]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, string defaultDeliveryAddress)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            // update phone number via identity user manager
            user.PhoneNumber = phoneNumber;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
