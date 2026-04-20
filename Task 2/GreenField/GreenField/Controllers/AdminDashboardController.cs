using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    [Authorize(Roles = "Admin")] // Admin dashboard — admins only
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminDashboard — shows all products and orders across all producers
        // Optionally filtered by a specific producer using the producerId query param
        public async Task<IActionResult> Index(int? producerId)
        {
            // Load all producers for the filter dropdown
            var producers = await _context.Producers.ToListAsync();

            List<Products> products;
            List<Orders> orders;

            if (producerId.HasValue)
            {
                // Filter products and orders to the selected producer only
                products = await _context.Products
                    .Where(p => p.ProducersId == producerId.Value)
                    .ToListAsync();

                orders = await _context.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Products)
                    .Where(o => o.OrderProducts.Any(op => op.Products.ProducersId == producerId.Value))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else
            {
                // No filter — load everything across all producers
                products = await _context.Products
                    .Include(p => p.Producers)
                    .ToListAsync();

                orders = await _context.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.Products)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            // Pass all data and summary stats to the view
            ViewBag.Producers = producers;
            ViewBag.SelectedProducerId = producerId;
            ViewBag.TotalProducts = products.Count;
            ViewBag.LowStockCount = products.Count(p => p.Stock <= 5);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalRevenue = orders.Sum(o => o.TotalPrice);
            ViewBag.Products = products;
            ViewBag.Orders = orders;

            return View();
        }
    }
}