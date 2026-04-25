using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagementSystem.Models;

namespace ProductionManagementSystem.Controllers
{
    [Route("api/lines")]
    [ApiController]
    public class LinesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LinesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/lines?available=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLines([FromQuery] bool available = false)
        {
            var query = _context.ProductionLines
                .Include(l => l.CurrentWorkOrder)
                .ThenInclude(w => w.Product)
                .AsQueryable();

            if (available)
            {
                query = query.Where(l => l.Status == "Active" && l.CurrentWorkOrderId == null);
            }

            var lines = await query.Select(l => new
            {
                id = l.Id,
                name = l.Name,
                status = l.Status,
                efficiencyFactor = l.EfficiencyFactor,
                currentWorkOrderId = l.CurrentWorkOrderId,
                currentProduct = l.CurrentWorkOrder != null ? l.CurrentWorkOrder.Product != null ? l.CurrentWorkOrder.Product.Name : null : null,
                currentProgress = l.CurrentWorkOrder != null ? l.CurrentWorkOrder.ProgressPercent : 0,
                currentOrderStatus = l.CurrentWorkOrder != null ? l.CurrentWorkOrder.Status : null
            }).ToListAsync();

            return Ok(lines);
        }

        // PUT /api/lines/{id}/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] StatusUpdateModel model)
        {
            var line = await _context.ProductionLines
                .Include(l => l.CurrentWorkOrder)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (line == null)
                return NotFound(new { message = "Линия не найдена" });

            line.Status = model.Status;
            
            // Если останавливаем линию и на ней есть заказ - ставим его на паузу
            if (model.Status == "Stopped" && line.CurrentWorkOrder != null)
            {
                if (line.CurrentWorkOrder.Status == "InProgress")
                {
                    line.CurrentWorkOrder.Status = "Paused";
                }
            }
            
            // Если запускаем линию и на ней есть заказ на паузе - возобновляем его
            if (model.Status == "Active" && line.CurrentWorkOrder != null)
            {
                if (line.CurrentWorkOrder.Status == "Paused")
                {
                    line.CurrentWorkOrder.Status = "InProgress";
                }
            }

            await _context.SaveChangesAsync();
            
            return Ok(new 
            { 
                id = line.Id, 
                status = line.Status,
                currentWorkOrderId = line.CurrentWorkOrderId,
                currentOrderStatus = line.CurrentWorkOrder?.Status
            });
        }

        // PUT /api/lines/{id}/efficiency
        [HttpPut("{id}/efficiency")]
        public async Task<ActionResult> UpdateEfficiency(int id, [FromBody] EfficiencyUpdateModel model)
        {
            var line = await _context.ProductionLines.FindAsync(id);
            if (line == null)
                return NotFound(new { message = "Линия не найдена" });

            // Проверяем диапазон
            if (model.EfficiencyFactor < 0.5f || model.EfficiencyFactor > 2.0f)
                return BadRequest(new { message = "Коэффициент эффективности должен быть в диапазоне от 0.5 до 2.0" });

            line.EfficiencyFactor = model.EfficiencyFactor;
            await _context.SaveChangesAsync();
            
            return Ok(new { id = line.Id, efficiencyFactor = line.EfficiencyFactor });
        }

        // GET /api/lines/{id}/schedule
        [HttpGet("{id}/schedule")]
        public async Task<ActionResult> GetSchedule(int id)
        {
            var schedule = await _context.WorkOrders
                .Include(w => w.Product)
                .Where(w => w.ProductionLineId == id)
                .OrderBy(w => w.StartDate)
                .Select(w => new
                {
                    id = w.Id,
                    productName = w.Product != null ? w.Product.Name : "Unknown",
                    quantity = w.Quantity,
                    startDate = w.StartDate,
                    estimatedEndDate = w.EstimatedEndDate,
                    status = w.Status,
                    progressPercent = w.ProgressPercent
                })
                .ToListAsync();

            return Ok(schedule);
        }
    }

    public class StatusUpdateModel
    {
        public string Status { get; set; } = string.Empty;
    }

    public class EfficiencyUpdateModel
    {
        public float EfficiencyFactor { get; set; }
    }
}