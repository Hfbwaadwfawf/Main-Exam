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

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel
            {
                FeaturedProducts = await _context.Products
                    .Include(p => p.Producers)
                    .Where(p => p.IsAvailable)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(4)
                    .ToListAsync(),

                FeaturedProducers = await _context.Producers
                    .Include(p => p.Products)
                    .Take(3)
                    .ToListAsync(),

                TotalProducers = await _context.Producers.CountAsync(),
                TotalProducts = await _context.Products.Where(p => p.IsAvailable).CountAsync()
            };

            return View(vm);
        }

        public IActionResult Privacy() => View();

        public IActionResult About() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}