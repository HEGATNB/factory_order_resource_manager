using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Materials()
        {
            var materials = await _context.Materials.ToListAsync();
            return View(materials);
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.ToListAsync();
            ViewBag.Categories = await _context.Products
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.WorkOrders
                .Include(w => w.Product)
                .Include(w => w.ProductionLine)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Lines()
        {
            var lines = await _context.ProductionLines
                .Include(l => l.CurrentWorkOrder)
                .ThenInclude(w => w!.Product)
                .ToListAsync();
            return View(lines);
        }
    }
}