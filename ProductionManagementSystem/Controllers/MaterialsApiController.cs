using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    [Route("api/materials")]
    [ApiController]
    public class MaterialsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaterialsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/materials?low_stock=true
        [HttpGet]
        public async Task<IActionResult> GetMaterials([FromQuery] bool low_stock = false)
        {
            var query = _context.Materials.AsQueryable();

            if (low_stock)
            {
                query = query.Where(m => m.Quantity <= m.MinimalStock);
            }

            var materials = await query.ToListAsync();
            return Ok(materials);
        }

        // POST /api/materials
        [HttpPost]
        public async Task<IActionResult> CreateMaterial([FromBody] Material material)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMaterials), new { id = material.Id }, material);
        }

        // PUT /api/materials/{id}/stock
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateModel model)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null)
                return NotFound();

            material.Quantity += model.Amount;
            await _context.SaveChangesAsync();
            return Ok(material);
        }
    }

    public class StockUpdateModel
    {
        public decimal Amount { get; set; }
    }
}