using System.Diagnostics;
using GreenField.Data;
using GreenField.Models;
using GreenField.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenField.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // inject logger and db context
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // home page — loads 4 random available products and up to 3 producers for the featured sections
        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel
            {
                // pick 4 random available products with their producer included
                FeaturedProducts = await _context.Products
                    .Include(p => p.Producers)
                    .Where(p => p.IsAvailable)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(4)
                    .ToListAsync(),

                // grab up to 3 producers with their products for the producer cards
                FeaturedProducers = await _context.Producers
                    .Include(p => p.Products)
                    .Take(3)
                    .ToListAsync(),

                // stats shown in the hero section
                TotalProducers = await _context.Producers.CountAsync(),
                TotalProducts = await _context.Products.Where(p => p.IsAvailable).CountAsync()
            };

            return View(vm);
        }

        // static privacy page
        public IActionResult Privacy() => View();

        // static about page
        public IActionResult About() => View();

        // error page — passes the request id so we can show it to the user
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
