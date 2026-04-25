using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;
using TouristGuide.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TouristGuide.Controllers
{
    public class CitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public CitiesController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: Cities
        public async Task<IActionResult> Index(string searchString)
        {
            var cities = from c in _context.Cities
                        select c;
            
            if (!string.IsNullOrEmpty(searchString))
            {
                cities = cities.Where(c => c.Name.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }
            
            return View(await cities.ToListAsync());
        }
        
        // GET: Cities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var city = await _context.Cities
                .Include(c => c.Attractions)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (city == null)
            {
                return NotFound();
            }
            
            return View(city);
        }
    }
}