using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

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

        // ============================================================
        // MAIN ENTRY POINT
        // Routes to the correct dashboard view based on role
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Admin));

            if (User.IsInRole("Producer"))
                return RedirectToAction(nameof(Producer));

            return RedirectToAction(nameof(Customer));
        }

        // ============================================================
        // CUSTOMER DASHBOARD
        // Shows own orders, basket summary, loyalty points
        // ============================================================
        public async Task<IActionResult> Customer()
        {
            var userId = _userManager.GetUserId(User);

            // Recent orders — last 5
            var recentOrders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // All orders for full history tab
            var allOrders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Basket summary
            var basket = await _context.Basket
                .Include(b => b.BasketProducts)
                    .ThenInclude(bp => bp.Products)
                        .ThenInclude(p => p.Producers)
                .FirstOrDefaultAsync(b => b.UserId == userId);

            // Loyalty points
            var loyaltyPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == userId);

            int points = loyaltyPoints?.Points ?? 0;

            // Order stats
            int totalOrders = allOrders.Count;
            int pendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending);
            int deliveredOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered);
            int cancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled);
            decimal totalSpent = allOrders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .Sum(o => o.TotalPrice);

            int basketItemCount = basket?.BasketProducts?.Sum(bp => bp.Quantity) ?? 0;

            ViewData["RecentOrders"] = recentOrders;
            ViewData["AllOrders"] = allOrders;
            ViewData["Basket"] = basket;
            ViewData["LoyaltyPoints"] = points;
            ViewData["PointsToNextReward"] = Math.Max(0, 300 - (points % 300));
            ViewData["TotalOrders"] = totalOrders;
            ViewData["PendingOrders"] = pendingOrders;
            ViewData["DeliveredOrders"] = deliveredOrders;
            ViewData["CancelledOrders"] = cancelledOrders;
            ViewData["TotalSpent"] = totalSpent;
            ViewData["BasketItemCount"] = basketItemCount;

            return View();
        }

        // PRODUCER DASHBOARD
        // Shows own products, orders containing their products,
        // stock levels, revenue summary
        [Authorize(Roles = "Producer")]
        public async Task<IActionResult> Producer()
        {
            var userId = _userManager.GetUserId(User);

            var producer = await _context.Producers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (producer == null)
            {
                TempData["Error"] = "No producer profile found for your account.";
                return RedirectToAction("Index", "Home");
            }

            // Producer's products
            var products = await _context.Products
                .Where(p => p.ProducersId == producer.ProducersId)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            // Orders containing this producer's products
            var orders = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                .Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == producer.ProducersId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Recent orders — last 5
            var recentOrders = orders.Take(5).ToList();

            // Stock summary
            int totalProducts = products.Count;
            int inStockCount = products.Count(p => p.Stock > 20);
            int lowStockCount = products.Count(p => p.Stock > 0 && p.Stock <= 20);
            int outOfStockCount = products.Count(p => p.Stock <= 0);

            // Order stats
            int totalOrders = orders.Count;
            int pendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            int processingOrders = orders.Count(o => o.Status == OrderStatus.Processing);
            int shippedOrders = orders.Count(o => o.Status == OrderStatus.OutForDelivery);
            int deliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);

            // Revenue — sum of order line items belonging to this producer only
            decimal totalRevenue = 0;
            foreach (var order in orders.Where(o => o.Status != OrderStatus.Cancelled))
            {
                foreach (var item in order.OrderProducts.Where(op => op.Products.ProducersId == producer.ProducersId))
                {
                    totalRevenue += item.Products.Price * item.Quantity;
                }
            }

            ViewData["Producer"] = producer;
            ViewData["Products"] = products;
            ViewData["AllOrders"] = orders;
            ViewData["RecentOrders"] = recentOrders;
            ViewData["TotalProducts"] = totalProducts;
            ViewData["InStockCount"] = inStockCount;
            ViewData["LowStockCount"] = lowStockCount;
            ViewData["OutOfStockCount"] = outOfStockCount;
            ViewData["TotalOrders"] = totalOrders;
            ViewData["PendingOrders"] = pendingOrders;
            ViewData["ProcessingOrders"] = processingOrders;
            ViewData["ShippedOrders"] = shippedOrders;
            ViewData["DeliveredOrders"] = deliveredOrders;
            ViewData["TotalRevenue"] = totalRevenue;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(int? filterProducerId, string? filterStatus, string? filterOrderType)
        {
            // All producers for filter dropdown
            var producers = await _context.Producers
                .OrderBy(p => p.BusinessName)
                .ToListAsync();

            // Orders query — filterable
            var ordersQuery = _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                        .ThenInclude(p => p.Producers)
                .AsQueryable();

            if (filterProducerId.HasValue)
                ordersQuery = ordersQuery.Where(o =>
                    o.OrderProducts.Any(op => op.Products.ProducersId == filterProducerId));

            if (!string.IsNullOrWhiteSpace(filterStatus) && Enum.TryParse<OrderStatus>(filterStatus, out var parsedStatus))
                ordersQuery = ordersQuery.Where(o => o.Status == parsedStatus);

            if (!string.IsNullOrWhiteSpace(filterOrderType))
            {
                if (filterOrderType == "delivery")
                    ordersQuery = ordersQuery.Where(o => o.IsDelivery);
                else if (filterOrderType == "collection")
                    ordersQuery = ordersQuery.Where(o => !o.IsDelivery);
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // All products filterable by producer
            var productsQuery = _context.Products
                .Include(p => p.Producers)
                .AsQueryable();

            if (filterProducerId.HasValue)
                productsQuery = productsQuery.Where(p => p.ProducersId == filterProducerId);

            var products = await productsQuery
                .OrderBy(p => p.Producers.BusinessName)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            // Platform-wide stats
            int totalUsers = await _userManager.Users.CountAsync();
            int totalProducers = producers.Count;
            int totalProducts = await _context.Products.CountAsync();
            int totalOrders = await _context.Orders.CountAsync();
            int pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            int lowStockProducts = await _context.Products.CountAsync(p => p.Stock > 0 && p.Stock <= 20);
            int outOfStockProducts = await _context.Products.CountAsync(p => p.Stock <= 0);
            decimal totalRevenue = await _context.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.TotalPrice);

            // Discount codes
            var discountCodes = await _context.DiscountCodes
                .OrderBy(d => d.Code)
                .ToListAsync();

            // Recent orders — last 10
            var recentOrders = orders.Take(10).ToList();

            // Low stock alert list
            var lowStockList = await _context.Products
                .Include(p => p.Producers)
                .Where(p => p.Stock <= 20)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            ViewData["Producers"] = producers;
            ViewData["ProducerSelectList"] = new SelectList(producers, "ProducersId", "BusinessName", filterProducerId);
            ViewData["FilterProducerId"] = filterProducerId;
            ViewData["FilterStatus"] = filterStatus;
            ViewData["FilterOrderType"] = filterOrderType;
            ViewData["AllOrders"] = orders;
            ViewData["RecentOrders"] = recentOrders;
            ViewData["AllProducts"] = products;
            ViewData["LowStockList"] = lowStockList;
            ViewData["DiscountCodes"] = discountCodes;
            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalProducers"] = totalProducers;
            ViewData["TotalProducts"] = totalProducts;
            ViewData["TotalOrders"] = totalOrders;
            ViewData["PendingOrders"] = pendingOrders;
            ViewData["LowStockProducts"] = lowStockProducts;
            ViewData["OutOfStockProducts"] = outOfStockProducts;
            ViewData["TotalRevenue"] = totalRevenue;

            return View();
        }

        // ============================================================
        // QUICK ACTIONS — shared across roles
        // ============================================================

        // POST: Dashboard/UpdateOrderStatus
        // Admin and Producer can update order status directly from dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) return NotFound();

            if (User.IsInRole("Producer"))
            {
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                var hasProduct = await _context.OrderProducts
                    .AnyAsync(op => op.OrdersId == orderId && op.Products.ProducersId == producer!.ProducersId);
                if (!hasProduct) return Forbid();
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Order status updated.";

            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Admin));

            return RedirectToAction(nameof(Producer));
        }

        // POST: Dashboard/UpdateStock
        // Producer or Admin updates stock level directly from dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> UpdateStock(int productId, int stock)
        {
            var userId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            if (User.IsInRole("Producer"))
            {
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || product.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            if (stock < 0)
            {
                TempData["Error"] = "Stock cannot be negative.";
                return RedirectToAction(nameof(Producer));
            }

            product.Stock = stock;
            product.IsAvailable = stock > 0;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Stock updated for {product.ProductName}.";

            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Admin));

            return RedirectToAction(nameof(Producer));
        }

        // POST: Dashboard/ToggleProductAvailability
        // Quickly toggle a product on or off without going to edit page
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Producer")]
        public async Task<IActionResult> ToggleProductAvailability(int productId)
        {
            var userId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            if (User.IsInRole("Producer"))
            {
                var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (producer == null || product.ProducersId != producer.ProducersId)
                    return Forbid();
            }

            product.IsAvailable = !product.IsAvailable;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{product.ProductName} is now {(product.IsAvailable ? "available" : "unavailable")}.";

            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Admin));

            return RedirectToAction(nameof(Producer));
        }

        // POST: Dashboard/ToggleDiscountCode
        // Admin quickly activates or deactivates a discount code from dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleDiscountCode(int discountCodeId)
        {
            var code = await _context.DiscountCodes.FindAsync(discountCodeId);
            if (code == null) return NotFound();

            code.IsActive = !code.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Code {code.Code} is now {(code.IsActive ? "active" : "inactive")}.";
            return RedirectToAction(nameof(Admin));
        }

        // POST: Dashboard/CancelOrder
        // Customer cancels their own pending order from dashboard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) return NotFound();
            if (order.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Only pending orders can be cancelled.";
                return RedirectToAction(nameof(Customer));
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Order cancelled.";
            return RedirectToAction(nameof(Customer));
        }

        // GET: Dashboard/OrderDetail/5
        // Any role can view order detail from their dashboard
        public async Task<IActionResult> OrderDetail(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Products)
                        .ThenInclude(p => p.Producers)
                .FirstOrDefaultAsync(o => o.OrdersId == id);

            if (order == null) return NotFound();

            // Access control
            if (!User.IsInRole("Admin") && order.UserId != userId)
            {
                if (User.IsInRole("Producer"))
                {
                    var producer = await _context.Producers.FirstOrDefaultAsync(p => p.UserId == userId);
                    bool hasProduct = order.OrderProducts.Any(op => op.Products.ProducersId == producer?.ProducersId);
                    if (!hasProduct) return Forbid();
                }
                else
                {
                    return Forbid();
                }
            }

            return View(order);
        }

        // GET: Dashboard/Profile
        // Any logged in user can view and edit their profile from dashboard
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var loyaltyPoints = await _context.LoyaltyPoints
                .FirstOrDefaultAsync(lp => lp.UserId == user.Id);

            ViewData["LoyaltyPoints"] = loyaltyPoints?.Points ?? 0;
            ViewData["Email"] = user.Email;
            ViewData["PhoneNumber"] = user.PhoneNumber;

            // If producer, also pass their producer profile
            if (User.IsInRole("Producer"))
            {
                var producer = await _context.Producers
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                ViewData["ProducerProfile"] = producer;
            }

            return View(user);
        }
    }
}