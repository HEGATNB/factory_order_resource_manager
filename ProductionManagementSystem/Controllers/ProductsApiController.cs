using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/products?category={id}
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? category = null)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var products = await query.ToListAsync();
            return Ok(products);
        }

        // GET /api/products/{id}/materials
        [HttpGet("{id}/materials")]
        public async Task<IActionResult> GetProductMaterials(int id)
        {
            var productMaterials = await _context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == id)
                .Select(pm => new
                {
                    pm.MaterialId,
                    pm.Material!.Name,
                    pm.QuantityNeeded,
                    pm.Material.UnitOfMeasure
                })
                .ToListAsync();

            if (!productMaterials.Any())
                return NotFound();

            return Ok(productMaterials);
        }

        // POST /api/products
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = new Product
            {
                Name = model.Name,
                ProductionTimePerUnit = model.ProdTime,
                Category = model.Category
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
        }
    }

    public class ProductCreateModel
    {
        public string Name { get; set; } = string.Empty;
        public int ProdTime { get; set; }
        public string? Category { get; set; }
    }
}