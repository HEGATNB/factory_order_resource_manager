using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    [Route("api/calculate")]
    [ApiController]
    public class CalculateApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CalculateApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST /api/calculate/production
        [HttpPost("production")]
        public async Task<IActionResult> CalculateProductionTime([FromBody] CalculationModel model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound("Продукт не найден");

            var totalMinutes = model.Quantity * product.ProductionTimePerUnit;
            var estimatedEnd = DateTime.Now.AddMinutes(totalMinutes);

            return Ok(new
            {
                ProductName = product.Name,
                Quantity = model.Quantity,
                TimePerUnit = product.ProductionTimePerUnit,
                TotalMinutes = totalMinutes,
                EstimatedEndDate = estimatedEnd
            });
        }
    }

    public class CalculationModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}